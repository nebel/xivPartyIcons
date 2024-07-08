using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Config;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using PartyIcons.Configuration;
using PartyIcons.Dalamud;
using PartyIcons.Entities;
using PartyIcons.Utils;
using PartyIcons.View;
using System.Linq;

namespace PartyIcons.Runtime;

public sealed class NameplateUpdater2 : IDisposable
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

    public NameplateUpdater2(NameplateView view)
    {
        _view = view;
    }

    private void SetReadyState(UpdaterState state, nint addonPtr = 0)
    {
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
                Service.NamePlateGui.OnChangedPlatesPreUpdate += OnChangedPlatesPreUpdate;
                Service.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "NamePlate", OnPostRequestedUpdate);
                break;
            case UpdaterState.Stopped:
                if (_updaterState == UpdaterState.Ready) {
                    Service.NamePlateGui.OnChangedPlatesPreUpdate -= OnChangedPlatesPreUpdate;
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

    private void OnChangedPlatesPreUpdate(PlateUpdateContext context, ReadOnlySpan<PlateUpdateHandler> handlers)
    {
        foreach (var handler in handlers) {
            if (handler.NamePlateKind == Dalamud.NamePlateKind.PlayerCharacter) {
                SetNamePlate(handler);
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
        if (_updaterState == UpdaterState.WaitingForDraw) {
            // Don't modify on first call as some setup may still be happening (seems to be cases where some node
            // siblings which shouldn't normally be null are null, usually when logging out/in during the same session)
            SetReadyState(UpdaterState.WaitingForNodes);
        }

        if (_updaterState == UpdaterState.WaitingForNodes) {
            try {
                if (CreateNodes((AddonNamePlate*)args.Addon)) {
                    SetReadyState(UpdaterState.Ready);
                }
            }
            catch (Exception e) {
                Service.Log.Error(e, "Failed to create nameplate icon nodes, will not try again");
                SetReadyState(UpdaterState.Stopped);
                SetReadyState(UpdaterState.Disabled);
            }

            Service.Framework.RunOnFrameworkThread(ForceRedrawNamePlates);
        }

        var isPvP = Service.ClientState.IsPvP;

        foreach (var state in _stateCache) {
            if (state.IsModified) {
                var obj = state.NamePlateObject;
                var kind = (NamePlateKind)obj->NamePlateKind;
                if (kind != NamePlateKind.Player || (obj->NameContainer->NodeFlags & NodeFlags.Visible) == 0 || isPvP) {
                    ResetPlate(state);
                }
                else {
                    // Copy UseDepthBasedPriority and Visible flags from NameTextNode
                    var nameFlags = obj->NameText->AtkResNode.NodeFlags;
                    if (state.UseExIcon)
                        state.ExIconNode->AtkResNode.NodeFlags ^=
                            (state.ExIconNode->AtkResNode.NodeFlags ^ nameFlags) &
                            (NodeFlags.UseDepthBasedPriority | NodeFlags.Visible);
                    if (state.UseSubIcon)
                        state.SubIconNode->AtkResNode.NodeFlags ^=
                            (state.SubIconNode->AtkResNode.NodeFlags ^ nameFlags) &
                            (NodeFlags.UseDepthBasedPriority | NodeFlags.Visible);

                    if (state.NeedsCollisionFix) {
                        var colScale = obj->NameText->AtkResNode.ScaleX * 2 * obj->NameContainer->ScaleX *
                                       state.CollisionScale;
                        var colRes = &obj->NameplateCollision->AtkResNode;
                        colRes->OriginX = colRes->Width / 2f;
                        colRes->OriginY = colRes->Height;
                        colRes->SetScale(colScale, colScale);
                        state.NeedsCollisionFix = false;
                    }
                }
            }

            // Debug icon padding by changing scale each frame
            // var scale = Service.Framework.LastUpdateUTC.Millisecond % 3000 / 500f * 4 + 1;
            // state.ExIconNode->AtkResNode.SetScale(scale, scale);
            // state.SubIconNode->AtkResNode.SetScale(scale, scale);
        }
    }


    private void SetNamePlate(PlateUpdateHandler handler)
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
            // TODO: DOES HIDE NEED TO WORK DIFFERENTLY NOW? (e.g. actually hide icons?)
            ResetPlate(state);
            return;
        }

        // _view.ModifyGlobalScale(state, context);
        // _view.ModifyNodes(state, context);
        state.IsModified = true;
        state.PendingChangesContext = context;
    }

    public static unsafe void ForceRedrawNamePlates()
    {
        Service.Log.Info("ForceRedrawNamePlates");
        var addon = (AddonNamePlate*)Service.GameGui.GetAddonByName("NamePlate");
        if (addon != null) {
            // Changing certain nameplate settings forces a call of the update function on the next frame, which checks
            // the full update flag and updates all visible plates. If we don't do the config part it may delay the
            // update for a short time or until the next camera movement.
            var setting = UiConfigOption.NamePlateDispJobIconType.ToString();
            var value = Service.GameConfig.UiConfig.GetUInt(setting);
            Service.GameConfig.UiConfig.Set(setting, value == 1u ? 0u : 1u);
            Service.GameConfig.UiConfig.Set(setting, value);
            addon->DoFullUpdate = 1;
        }

        // var m = RaptureAtkModule.Instance();
        // var array = m->AtkArrayDataHolder.NumberArrays[5];
        // array->SetValue(4, 1);
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

        state.ExIconNode->AtkResNode.ToggleVisibility(false);
        state.SubIconNode->AtkResNode.ToggleVisibility(false);

        state.NamePlateObject->NameText->AtkResNode.SetScale(0.5f, 0.5f);

        state.NamePlateObject->NameplateCollision->AtkResNode.OriginX = 0;
        state.NamePlateObject->NameplateCollision->AtkResNode.OriginY = 0;
        state.NamePlateObject->NameplateCollision->AtkResNode.SetScale(1f, 1f);

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

        imageNode->AtkResNode.NodeFlags = NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.Enabled |
                                          NodeFlags.EmitsEvents | NodeFlags.UseDepthBasedPriority;
        imageNode->AtkResNode.SetWidth(32);
        imageNode->AtkResNode.SetHeight(32);

        imageNode->WrapMode = 1;
        imageNode->Flags = (byte)ImageNodeFlags.AutoFit;
        imageNode->LoadIconTexture(60071, 0);

        // var targetNode = AtkHelper.GetNodeByID<AtkResNode>(&parent->Component->UldManager, targetNodeId);
        // if (targetNode == null) {
        //     throw new Exception($"Failed to find link target ({targetNodeId}) for image node {nodeId}");
        // }
        //
        // AtkHelper.LinkNodeAfterTargetNode((AtkResNode*)imageNode, parent, targetNode);

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

    // 2: Unknown. Shares the same internal 'renderer' as 1 & 5.
    // 4: Seems to be used by any friendly NPC with a level in its nameplate. NPCs of type 1 can become type 4 when a
    //    FATE triggers, for example (e.g. Boughbury Trader -> Lv32 Boughbury Trader). Shares same internal 'renderer'
    //    as 3.
    // 6: Unknown.
    // 7: Unknown.
    // 8: Unknown.
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private enum NamePlateKind : byte
    {
        Player = 0,
        FriendlyNpc = 1,
        Unknown2 = 2,
        Enemy = 3,
        FriendlyCombatant = 4,
        Interactable = 5,
        Unknown6 = 6,
        Unknown7 = 7,
        Unknown8 = 8,
    }
}