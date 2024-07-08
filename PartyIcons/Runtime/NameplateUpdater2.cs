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
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using PartyIcons.Configuration;
using PartyIcons.Dalamud;
using PartyIcons.Entities;
using PartyIcons.Utils;
using PartyIcons.View;

namespace PartyIcons.Runtime;

public sealed class NameplateUpdater2 : IDisposable
{
    private readonly NameplateView _view;

    private const uint EmptyIconId = 4294967295; // (uint)-1
    private const uint PlaceholderEmptyIconId = 61696;

    private const int NameTextNodeId = 3;
    private const int IconNodeId = 4;
    private const int ExNodeId = 8004;
    private const int SubNodeId = 8005;

    private static UpdaterState _updaterState = UpdaterState.Uninitialized;
    private static PlateState[] _stateCache = [];

    private enum UpdaterState
    {
        Uninitialized,
        Initializing,
        Ready,
        Stopped
    }

    public NameplateUpdater2(NameplateView view)
    {
        _view = view;
    }

    public void Enable()
    {
        Service.Log.Warning("ENABLE");
        Plugin.RoleTracker.OnAssignedRolesUpdated += ForceRedrawNamePlates;
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "NamePlate", OnPreFinalize);
        Service.NamePlateGui.OnChangedPlatesPreUpdate += OnChangedPlatedPreUpdate;
        Service.NamePlateGui.OnChangedPlatesPreDraw += OnChangedPlatedPreDraw;
    }

    public unsafe void OnChangedPlatedPreDraw(PlateUpdateContext context, ReadOnlySpan<PlateUpdateHandler> handlers)
    {
        // Service.Log.Warning($"OnChangedPlatedPreDraw ({handlers.Length})");
        AddonNamePlateDrawDetour(context.addon);
        // throw new NotImplementedException();
    }

    public void OnChangedPlatedPreUpdate(PlateUpdateContext context, ReadOnlySpan<PlateUpdateHandler> handlers)
    {
        // Service.Log.Warning($"OnChangedPlatedPreUpdate ({handlers.Length})");
        foreach (var handler in handlers) {
            if (handler.NamePlateKind == 0) {
                SetNamePlate(context, handler);
            }
            // handler.TextColor = 0xFF00FF00;
            // handler.TextColor = 0;
        }
    }

    public void Dispose()
    {
        Service.Log.Warning("DISPOSE");
        Plugin.RoleTracker.OnAssignedRolesUpdated -= ForceRedrawNamePlates;
        Service.AddonLifecycle.UnregisterListener(OnPreFinalize);

        _updaterState = UpdaterState.Stopped;

        unsafe {
            var addonPtr = (AddonNamePlate*)Service.GameGui.GetAddonByName("NamePlate");
            if (addonPtr != null) {
                ResetAllPlates();
                DestroyAllNodes(addonPtr);
            }
        }
    }

    private static unsafe void OnPreFinalize(AddonEvent type, AddonArgs args)
    {
        Service.Log.Debug($"OnPreFinalize (0x{args.Addon:X})");

        ResetAllPlates();
        DestroyAllNodes((AddonNamePlate*)args.Addon);

        _updaterState = UpdaterState.Uninitialized;
        _stateCache = [];
        new Dictionary<nint, int>().ToFrozenDictionary();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe IPlayerCharacter? ResolvePlayerCharacter(GameObjectId gameObjectId)
    {
        if (gameObjectId.Type != 0) {
            return null;
        }

        if (Service.ObjectTable.SearchById(gameObjectId) is IPlayerCharacter c) {
            var job = ((Character*)c.Address)->CharacterData.ClassJob;
            return job is < 1 or > JobConstants.MaxJob ? null : c;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe Character* ResolveCharacter3D(GameObjectId objectId)
    {
        if (objectId.Type != 0) {
            return null;
        }

        var ui3DModule = UIModule.Instance()->GetUI3DModule();
        for (var i = 0; i < ui3DModule->NamePlateObjectInfoCount; i++) {
            var objectInfo = ui3DModule->NamePlateObjectInfoPointers[i].Value;
            var obj = objectInfo->GameObject;
            if (obj->GetGameObjectId() == objectId && obj->ObjectKind == ObjectKind.Pc) {
                var character = (Character*)obj;
                return character->CharacterData.ClassJob is < 1 or > JobConstants.MaxJob ? null : character;
            }
        }

        return null;
    }

    private unsafe void AddonNamePlateDrawDetour(AddonNamePlate* addon)
    {
        if (_updaterState == UpdaterState.Uninitialized) {
            // Don't modify on first call as some setup may still be happening (seems to be cases where some node
            // siblings which shouldn't normally be null are null, usually when logging out/in during the same session)
            _updaterState = UpdaterState.Initializing;
        }

        if (_updaterState == UpdaterState.Initializing) {
            try {
                if (CreateNodes(addon)) {
                    _updaterState = UpdaterState.Ready;
                }
            }
            catch (Exception e) {
                Service.Log.Error(e, "Failed to create nameplate icon nodes, will not try again");
                _updaterState = UpdaterState.Stopped;
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
        } ;
    }


    private unsafe void SetNamePlate(PlateUpdateContext updateContext, PlateUpdateHandler handler)
    {
        if (_updaterState != UpdaterState.Ready) {
            return;
        }
        // var prefixByte = ((byte*)prefix)[0];
        // var prefixIcon = BitmapFontIcon.None;
        // if (prefixByte != 0) {
        //     prefixIcon = ((IconPayload)MemoryHelper.ReadSeStringNullTerminated(prefix).Payloads[1]).Icon;
        // }
        // Service.Log.Warning(
        //     $"SetNamePlate @ 0x{namePlateObjectPtr:X}\nTitle: isPrefix=[{isPrefixTitle}] displayTitle=[{displayTitle}] title=[{SeStringUtils.PrintRawStringArg(title)}]\n" +
        //     $"name=[{SeStringUtils.PrintRawStringArg(name)}] fcName=[{SeStringUtils.PrintRawStringArg(fcName)}] prefix=[{SeStringUtils.PrintRawStringArg(prefix)}] iconID=[{iconID}]\n" +
        //     $"prefixByte=[0x{prefixByte:X}] prefixIcon=[{prefixIcon}({(int)prefixIcon})]");

        var atkModule = RaptureAtkModule.Instance();
        if (atkModule == null) {
            throw new Exception("Unable to resolve NamePlate character as RaptureAtkModule was null");
        }

        var index = handler.NamePlateIndex;
        var state = _stateCache[index];
        var info = atkModule->NamePlateInfoEntries.GetPointer(index);

        if (Service.ClientState.IsPvP || info == null) {
            ResetPlate(state);
            return;
        }

        if (ResolvePlayerCharacter(info->ObjectId) is not { } playerCharacter) {
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

        // Replace 0/-1 with empty dummy texture so the default icon is always positioned even for unselected
        // targets (when unselected targets are hidden). If we don't do this, the icon node will only be
        // positioned by the game after the target is selected for hidden nameplates, which would force us to
        // re-position after the initial SetNamePlate call (which would be very annoying).
        // iconId = PlaceholderEmptyIconId;

        if (context.Mode == NameplateMode.Hide) {
            // TODO: DOES HIDE NEED TO WORK DIFFERENTLY NOW? (e.g. actually hide icons?)
            ResetPlate(state);
            return;
        }

        // _view.ModifyGlobalScale(state, context);
        // _view.ModifyNodes(state, context);
        state.IsModified = true;
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
                exNode = CreateImageNode(ExNodeId, componentNode, IconNodeId);
            }

            var subNode =
                AtkHelper.GetNodeByID<AtkImageNode>(uldManager, SubNodeId, NodeType.Image);
            if (subNode == null) {
                subNode = CreateImageNode(SubNodeId, componentNode, ExNodeId);
                // subNode = CreateImageNode(SubNodeId, componentNode, NameTextNodeId);
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

    private static unsafe AtkImageNode* CreateImageNode(uint nodeId, AtkComponentNode* parent, uint targetNodeId)
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

        var targetNode = AtkHelper.GetNodeByID<AtkResNode>(&parent->Component->UldManager, targetNodeId);
        if (targetNode == null) {
            throw new Exception($"Failed to find link target ({targetNodeId}) for image node {nodeId}");
        }

        AtkHelper.LinkNodeAfterTargetNode((AtkResNode*)imageNode, parent, targetNode);

        return imageNode;
    }

    private static unsafe void DestroyAllNodes(AddonNamePlate* addon)
    {
        Service.Log.Debug("DestroyNodes");

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