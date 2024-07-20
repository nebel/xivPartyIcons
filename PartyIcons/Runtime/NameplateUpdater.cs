using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Gui.NamePlate;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PartyIcons.Configuration;
using PartyIcons.Utils;
using PartyIcons.View;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace PartyIcons.Runtime;

public sealed class NameplateUpdater : IDisposable
{
    private readonly NameplateView _view;

    private const int NameTextNodeId = 3;
    private const int IconNodeId = 4;
    private const int ExNodeId = 8004;
    private const int SubNodeId = 8005;

    private static UpdaterState _updaterState = UpdaterState.Uninitialized;
    private static PlateState[] _stateCache = [];

    private enum UpdaterState
    {
        Uninitialized,
        Enabled,
        WaitingForDraw,
        WaitingForNodes,
        Ready,
        Stopped,
        Disabled
    }

    public NameplateUpdater(NameplateView view)
    {
        _view = view;
    }

    private void SetReadyState(UpdaterState state, nint addonPtr = 0)
    {
        Service.Log.Debug($"SetReadyState: {_updaterState} -> {state}");
        switch (state) {
            case UpdaterState.Enabled:
                Plugin.RoleTracker.OnAssignedRolesUpdated += ForceRedrawNamePlates;
                Service.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "NamePlate", OnPreDraw);
                Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "NamePlate", OnPreFinalize);
                break;
            case UpdaterState.WaitingForDraw:
                _updaterState = UpdaterState.WaitingForDraw;
                _stateCache = [];
                break;
            case UpdaterState.WaitingForNodes:
                break;
            case UpdaterState.Ready:
                Service.NamePlateGui.OnNamePlateUpdate += OnNamePlateUpdate;
                Service.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "NamePlate", OnPostRequestedUpdate);
                break;
            case UpdaterState.Stopped:
                if (_updaterState == UpdaterState.Ready) {
                    Service.NamePlateGui.OnNamePlateUpdate -= OnNamePlateUpdate;
                    Service.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "NamePlate", OnPostRequestedUpdate);
                }
                if (addonPtr == 0) {
                    addonPtr = Service.GameGui.GetAddonByName("NamePlate");
                }
                if (addonPtr != 0) {
                    ResetAllPlates();
                    DestroyAllNodes(addonPtr);
                }
                break;
            case UpdaterState.Disabled:
                Plugin.RoleTracker.OnAssignedRolesUpdated -= ForceRedrawNamePlates;
                Service.AddonLifecycle.UnregisterListener(AddonEvent.PreDraw, "NamePlate", OnPreDraw);
                Service.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "NamePlate", OnPreFinalize);
                break;
            case UpdaterState.Uninitialized:
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        _updaterState = state;
    }

    public void Enable()
    {
        SetReadyState(UpdaterState.Enabled);
        SetReadyState(UpdaterState.WaitingForDraw);
    }

    public void Dispose()
    {
        SetReadyState(UpdaterState.Stopped);
        SetReadyState(UpdaterState.Disabled);
    }

    private void OnPreFinalize(AddonEvent type, AddonArgs args)
    {
        SetReadyState(UpdaterState.Stopped);
        SetReadyState(UpdaterState.WaitingForDraw);
    }

    private void OnNamePlateUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        foreach (var handler in handlers) {
            if (handler.NamePlateKind == NamePlateKind.PlayerCharacter) {
                SetNamePlate(handler);
            }
            else {
                var index = handler.NamePlateIndex;
                var state = _stateCache[index];
                if (state.IsModified) {
                    ResetPlate(state);
                }
            }
        }
    }

    private void OnPostRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        foreach (var state in _stateCache) {
            _view.DoPendingChanges(state);
        }
    }

    private unsafe void OnPreDraw(AddonEvent type, AddonArgs args)
    {
        if (_updaterState != UpdaterState.Ready) {
            if (_updaterState == UpdaterState.WaitingForDraw) {
                // Don't modify on first call as some setup may still be happening (seems to be cases where some node
                // siblings which shouldn't normally be null are null, usually when logging out/in during the same session)
                SetReadyState(UpdaterState.WaitingForNodes);
            }
            else if (_updaterState == UpdaterState.WaitingForNodes) {
                try {
                    if (CreateNodes((AddonNamePlate*)args.Addon)) {
                        SetReadyState(UpdaterState.Ready);
                        Service.Framework.RunOnFrameworkThread(ForceRedrawNamePlates);
                    }
                }
                catch (Exception e) {
                    Service.Log.Error(e, "Failed to create nameplate icon nodes, will not try again");
                    SetReadyState(UpdaterState.Stopped);
                    SetReadyState(UpdaterState.Disabled);
                }
            }
            return;
        }

        var isPvP = Service.ClientState.IsPvP;

        foreach (var state in _stateCache) {
            if (state.IsModified) {
                var obj = state.NamePlateObject;
                var kind = obj->NamePlateKind;
                if (kind != UIObjectKind.PlayerCharacter) {
                    ResetPlate(state);
                    return;
                }
                if ((obj->RootComponentNode->NodeFlags & NodeFlags.Visible) == 0) {
                    ResetPlate(state);
                    return;
                }
                if (isPvP) {
                    ResetPlate(state);
                    return;
                }

                // Copy UseDepthBasedPriority and Visible flags from NameTextNode
                var nameFlags = obj->NameText->NodeFlags;
                if (state.UseExIcon)
                    state.ExIconNode->NodeFlags ^=
                        (state.ExIconNode->NodeFlags ^ nameFlags) &
                        (NodeFlags.UseDepthBasedPriority | NodeFlags.Visible);
                if (state.UseSubIcon)
                    state.SubIconNode->NodeFlags ^=
                        (state.SubIconNode->NodeFlags ^ nameFlags) &
                        (NodeFlags.UseDepthBasedPriority | NodeFlags.Visible);
                if (state.NeedsCollisionFix) {
                    var colScale = obj->NameText->ScaleX * 2 * obj->NameContainer->ScaleX *
                                   state.CollisionScale;
                    var colRes = obj->NameplateCollision;
                    colRes->OriginX = colRes->Width / 2f;
                    colRes->OriginY = colRes->Height;
                    colRes->SetScale(colScale, colScale);
                    state.NeedsCollisionFix = false;
                }
            }

            // Debug icon padding by changing scale each frame
            // var scale = Service.Framework.LastUpdateUTC.Millisecond % 3000 / 500f * 4 + 1;
            // state.ExIconNode->SetScale(scale, scale);
            // state.SubIconNode->SetScale(scale, scale);
        }
    }

    private void SetNamePlate(INamePlateUpdateHandler handler)
    {
        var index = handler.NamePlateIndex;
        var state = _stateCache[index];

        if (Service.ClientState.IsPvP) {
            ResetPlate(state);
            return;
        }

        if (handler.PlayerCharacter is not { } playerCharacter) {
            ResetPlate(state);
            return;
        }

        var context = new UpdateContext(playerCharacter);
        _view.UpdateViewData(ref context);

        if (context.Mode == NameplateMode.Default) {
            ResetPlate(state);
            return;
        }

        _view.ModifyPlateData(context, handler);

        if (context.Mode == NameplateMode.Hide) {
            ResetPlate(state);
            return;
        }

        state.IsModified = true;
        state.PendingChangesContext = context;
    }

    public static void ForceRedrawNamePlates()
    {
        Service.Log.Debug("ForceRedrawNamePlates");
        Service.NamePlateGui.RequestRedraw();
    }

    private static void ResetAllPlates()
    {
        foreach (var state in _stateCache) {
            ResetPlate(state);
        }
    }

    private static unsafe void ResetPlate(PlateState state)
    {
        if (state.IsGlobalScaleModified) {
            state.NamePlateObject->NameContainer->OriginX = 0;
            state.NamePlateObject->NameContainer->OriginY = 0;
            state.NamePlateObject->NameContainer->SetScale(1f, 1f);
        }

        state.ExIconNode->ToggleVisibility(false);
        state.SubIconNode->ToggleVisibility(false);

        state.NamePlateObject->NameText->SetScale(0.5f, 0.5f);

        state.NamePlateObject->NameplateCollision->OriginX = 0;
        state.NamePlateObject->NameplateCollision->OriginY = 0;
        state.NamePlateObject->NameplateCollision->SetScale(1f, 1f);

        state.IsModified = false;
        state.PendingChangesContext = null;
    }

    private static unsafe bool CreateNodes(AddonNamePlate* addon)
    {
        Service.Log.Debug("CreateNodes");

        var stateCache = new PlateState[AddonNamePlate.NumNamePlateObjects];
        var indexMap = new Dictionary<nint, int>();

        var arr = addon->NamePlateObjectArray;
        if (arr == null) return false;

        for (var i = 0; i < AddonNamePlate.NumNamePlateObjects; i++) {
            var np = &arr[i];
            var resNode = np->NameContainer;
            var componentNode = resNode->ParentNode->GetAsAtkComponentNode();
            var uldManager = &componentNode->Component->UldManager;

            if (uldManager->LoadedState != AtkLoadState.Loaded) {
                return false;
            }

            var exNode =
                AtkHelper.GetNodeByID<AtkImageNode>(uldManager, ExNodeId, NodeType.Image);
            if (exNode == null) {
                exNode = CreateImageNode(ExNodeId);
                var targetNode = AtkHelper.GetNodeByID<AtkResNode>(&componentNode->Component->UldManager, IconNodeId);
                if (targetNode == null) {
                    throw new Exception($"Failed to find link target ({IconNodeId}) for image node {IconNodeId}");
                }
                AtkHelper.LinkNodeAfterTargetNode((AtkResNode*)exNode, componentNode, targetNode);
            }

            var subNode =
                AtkHelper.GetNodeByID<AtkImageNode>(uldManager, SubNodeId, NodeType.Image);
            if (subNode == null) {
                subNode = CreateImageNode(SubNodeId);
                var targetNode = AtkHelper.GetNodeByID<AtkResNode>(&componentNode->Component->UldManager, NameTextNodeId);
                if (targetNode == null) {
                    throw new Exception($"Failed to find link target ({NameTextNodeId}) for image node {NameTextNodeId}");
                }
                AtkHelper.LinkNodeAtEnd((AtkResNode*)subNode, componentNode, resNode);
            }

            var namePlateObjectPointer = arr + i;

            stateCache[i] = new PlateState
            {
                NamePlateObject = namePlateObjectPointer,
                ExIconNode = exNode,
                SubIconNode = subNode,
                UseExIcon = false,
                UseSubIcon = true,
            };
            indexMap[(nint)namePlateObjectPointer] = i;
        }

        _stateCache = stateCache;
        indexMap.ToFrozenDictionary();

        return true;
    }

    private static unsafe AtkImageNode* CreateImageNode(uint nodeId)
    {
        var imageNode = AtkHelper.MakeImageNode(nodeId, new AtkHelper.PartInfo(0, 0, 32, 32));
        if (imageNode == null) {
            throw new Exception($"Failed to create image node {nodeId}");
        }

        imageNode->NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.Enabled |
                               NodeFlags.EmitsEvents | NodeFlags.UseDepthBasedPriority;
        imageNode->SetWidth(32);
        imageNode->SetHeight(32);

        imageNode->WrapMode = 1;
        imageNode->Flags = (byte)ImageNodeFlags.AutoFit;
        imageNode->LoadIconTexture(60071, 0);

        return imageNode;
    }

    private static unsafe void DestroyAllNodes(nint addonPtr)
    {
        Service.Log.Debug("DestroyNodes");

        var addon = (AddonNamePlate*)addonPtr;

        var arr = addon->NamePlateObjectArray;
        if (arr == null) return;

        for (var i = 0; i < AddonNamePlate.NumNamePlateObjects; i++) {
            var np = arr[i];
            var resNode = np.NameContainer;
            if (resNode == null) continue;
            var parentNode = resNode->ParentNode;
            if (parentNode == null) continue;
            var parentComponentNode = parentNode->GetAsAtkComponentNode();
            if (parentComponentNode == null) continue;
            var parentComponentNodeComponent = parentComponentNode->Component;
            if (parentComponentNodeComponent == null) continue;

            var exNode =
                AtkHelper.GetNodeByID<AtkImageNode>(&parentComponentNodeComponent->UldManager, ExNodeId, NodeType.Image);
            if (exNode != null) {
                AtkHelper.UnlinkAndFreeImageNodeIndirect(exNode, &parentComponentNodeComponent->UldManager);
            }

            var subNode = AtkHelper.GetNodeByID<AtkImageNode>(&parentComponentNodeComponent->UldManager, SubNodeId,
                NodeType.Image);
            if (subNode != null) {
                AtkHelper.UnlinkAndFreeImageNodeIndirect(subNode, &parentComponentNodeComponent->UldManager);
            }
        }
    }
}