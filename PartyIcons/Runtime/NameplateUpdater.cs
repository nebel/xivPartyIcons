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
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.Utils;
using PartyIcons.View;

namespace PartyIcons.Runtime;

public sealed class NameplateUpdater : IDisposable
{
    private readonly NameplateView _view;

    [Signature("0F B7 81 ?? ?? ?? ?? 4C 8B C1 66 C1 E0 06", DetourName = nameof(AddonNamePlateDrawDetour))]
    private readonly Hook<AddonNamePlateDrawDelegate> _namePlateDrawHook = null!;

    [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 5C 24 ?? 45 38 BE", DetourName = nameof(SetNamePlateDetour))]
    private readonly Hook<SetNamePlateDelegate> _setNamePlateHook = null!;

    private unsafe delegate void AddonNamePlateDrawDelegate(AddonNamePlate* thisPtr);

    private delegate IntPtr SetNamePlateDelegate(IntPtr addon, bool isPrefixTitle, bool displayTitle, IntPtr title,
        IntPtr name, IntPtr fcName, IntPtr prefix, uint iconID);

    private const uint EmptyIconId = 4294967295; // (uint)-1
    private const uint PlaceholderEmptyIconId = 61696;

    private const int CollisionNodeId = 11;
    private const int NameTextNodeId = 3;
    private const int IconNodeId = 4;
    private const int ExNodeId = 8004;
    private const int SubNodeId = 8005;

    private static nint _addonNamePlateAddr;
    private static PlateState[] _stateCache = [];
    private static FrozenDictionary<nint, int> _indexMap = new Dictionary<nint, int>().ToFrozenDictionary();

    public int DebugIcon { get; set; } = -1;

    public NameplateUpdater(Settings configuration, NameplateView view, ViewModeSetter modeSetter)
    {
        _view = view;
        Service.GameInteropProvider.InitializeFromAttributes(this);
    }

    public void Enable()
    {
        _setNamePlateHook.Enable();
        _namePlateDrawHook.Enable();

        Plugin.RoleTracker.OnAssignedRolesUpdated += ForceRedrawNamePlates;
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "NamePlate", OnPreFinalize);
    }

    private static unsafe void OnPreFinalize(AddonEvent type, AddonArgs args)
    {
        Service.Log.Info($"OnPreFinalize (0x{args.Addon:X})");
        ResetAllNodes();
        DestroyNodes((AddonNamePlate*)args.Addon);
        _addonNamePlateAddr = 0;
    }

    public void Dispose()
    {
        _setNamePlateHook.Disable();
        _setNamePlateHook.Dispose();
        _namePlateDrawHook.Disable();
        _namePlateDrawHook.Dispose();

        Plugin.RoleTracker.OnAssignedRolesUpdated -= ForceRedrawNamePlates;
        Service.AddonLifecycle.UnregisterListener(OnPreFinalize);

        unsafe {
            var addonPtr = (AddonNamePlate*)Service.GameGui.GetAddonByName("NamePlate");
            if (addonPtr != null) {
                ResetAllNodes();
                DestroyNodes(addonPtr);
            }
        }
    }

    public IntPtr SetNamePlateDetour(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle,
        IntPtr title, IntPtr name, IntPtr fcName, IntPtr prefix, uint iconID)
    {
        var hookResult = IntPtr.MinValue;
        if (_addonNamePlateAddr > 0) {
            try {
                SetNamePlate(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, prefix, iconID,
                    ref hookResult);
            }
            catch (Exception ex) {
                Service.Log.Error(ex, "SetNamePlateDetour encountered a critical error");
            }
        }

        return hookResult == IntPtr.MinValue
            ? _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, prefix,
                iconID)
            : hookResult;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe PlayerCharacter? ResolvePlayerCharacter(uint objectId)
    {
        if (objectId == 0xE0000000) {
            return null;
        }

        foreach (var obj in Service.ObjectTable) {
            if (obj.ObjectId == objectId && obj is PlayerCharacter c) {
                var job = ((Character*)c.Address)->CharacterData
                    .ClassJob;
                return job is < 1 or > JobConstants.MaxJob ? null : c;
            }
        }

        return null;
    }

    private unsafe void SetNamePlate(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle, IntPtr title,
        IntPtr name, IntPtr fcName, IntPtr prefix, uint iconID, ref IntPtr hookResult)
    {
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

        var index = _indexMap[namePlateObjectPtr];
        var state = _stateCache[index];
        var info = atkModule->NamePlateInfoEntriesSpan.GetPointer(index);

        if (Service.ClientState.IsPvP || info == null) {
            ResetNodes(state);
            return;
        }

        var objectId = info->ObjectID.ObjectID;
        if (ResolvePlayerCharacter(objectId) is not { } playerCharacter) {
            ResetNodes(state);
            return;
        }

        var context = new UpdateContext(playerCharacter);
        _view.UpdateViewData(ref context);

        var originalTitle = title;
        var originalName = name;
        var originalFcName = fcName;
        var originalPrefix = prefix;

        if (context.Mode == NameplateMode.RoleLetters) {
            context.ShowExIcon = true;
            context.ShowStatusIcon = true;
        }

        try {
            _view.ModifyParameters(context, ref isPrefixTitle, ref displayTitle, ref title, ref name, ref fcName,
                ref prefix, ref iconID);
        }
        finally {
            if (originalName != name)
                SeStringUtils.FreePtr(name);
            if (originalTitle != title)
                SeStringUtils.FreePtr(title);
            if (originalFcName != fcName)
                SeStringUtils.FreePtr(fcName);
            if (originalPrefix != prefix)
                SeStringUtils.FreePtr(prefix);
        }

        // Service.Log.Info($"{state.NamePlateObject->NameText->NodeText} -> {state.NamePlateObject->TextW}");

        if (iconID is EmptyIconId or 0) {
            // Replace 0/-1 with empty dummy texture so the default icon is always positioned even for unselected
            // targets (when unselected targets are hidden). If we don't do this, the icon node will only be positioned
            // after the target is selected, which forces us to re-position.
            iconID = PlaceholderEmptyIconId;
            state.IsIconBlank = true;
        }
        else {
            state.IsIconBlank = false;
        }

        hookResult = _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName,
            prefix, iconID);

        _view.ModifyGlobalScale(state);
        _view.ModifyNodes(state, context);
        state.IsModified = true;
        state.IsCollisionFixed = false;
    }

    private static unsafe void CreateNodes(AddonNamePlate* addon)
    {
        Service.Log.Error("CreateNodes");

        var stateCache = new PlateState[AddonNamePlate.NumNamePlateObjects];
        var indexMap = new Dictionary<nint, int>();

        var arr = addon->NamePlateObjectArray;
        if (arr == null) return;

        for (var i = 0; i < AddonNamePlate.NumNamePlateObjects; i++) {
            var np = arr[i];
            var resNode = np.ResNode;
            var componentNode = resNode->ParentNode->GetAsAtkComponentNode();
            var uldManager = &componentNode->Component->UldManager;

            var collisionNode =
                UiHelper.GetNodeByID<AtkCollisionNode>(uldManager, CollisionNodeId, NodeType.Collision);

            var textNode =
                UiHelper.GetNodeByID<AtkTextNode>(uldManager, NameTextNodeId, NodeType.Text);
            var iconNode =
                UiHelper.GetNodeByID<AtkImageNode>(uldManager, IconNodeId, NodeType.Image);

            var exNode =
                UiHelper.GetNodeByID<AtkImageNode>(uldManager, ExNodeId, NodeType.Image);
            if (exNode == null) {
                exNode = CreateImageNode(ExNodeId, componentNode, IconNodeId);
            }

            var subNode =
                UiHelper.GetNodeByID<AtkImageNode>(uldManager, SubNodeId, NodeType.Image);
            if (subNode == null) {
                subNode = CreateImageNode(SubNodeId, componentNode, NameTextNodeId);
            }

            var namePlateObjectPointer = arr + i;

            stateCache[i] = new PlateState
            {
                NamePlateObject = namePlateObjectPointer,
                CollisionNode = collisionNode,
                ResNode = resNode,
                NameTextNode = textNode,
                IconNode = iconNode,
                ExIconNode = exNode,
                SubIconNode = subNode,
                IsIconBlank = false,
                UseExIcon = false,
                UseSubIcon = true,
            };
            indexMap[(nint)namePlateObjectPointer] = i;
        }

        // for (var i = 0; i < infoArray.Length; i++) {
        //     var info = infoArray[i];
        //     if (info.ComponentNode->AtkResNode.IsVisible) {
        //         Service.Log.Info(
        //             $"  {i} -> {(info.ComponentNode->AtkResNode.IsVisible ? 'v' : 'i')} {info} ({info.NamePlateObject->NameText->NodeText.ToString().Replace("\n", "\\n")} / {info.NamePlateObject->NameplateKind})");
        //     }
        // }

        _stateCache = stateCache;
        _indexMap = indexMap.ToFrozenDictionary();
        _addonNamePlateAddr = (nint)addon;
    }

    private unsafe void AddonNamePlateDrawDetour(AddonNamePlate* addon)
    {
        if (addon->NamePlateObjectArray == null) return;

        if (_addonNamePlateAddr == 0) {
            try {
                CreateNodes(addon);
            }
            catch (Exception e) {
                Service.Log.Error(e, "Failed to create nameplate icon nodes, will not try again");
                _addonNamePlateAddr = -1;
            }

            Service.Framework.RunOnFrameworkThread(ForceRedrawNamePlates);
        }

        var isPvP = Service.ClientState.IsPvP;

        foreach (var state in _stateCache) {
            if (state.IsModified) {
                var obj = state.NamePlateObject;
                var kind = (NamePlateKind)obj->NameplateKind;
                if (kind != NamePlateKind.Player || (obj->ResNode->NodeFlags & NodeFlags.Visible) == 0 || isPvP) {
                    ResetNodes(state);
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

                    if (!state.IsCollisionFixed) {
                        // var exIconScale = state.ExIconNode->AtkResNode.ScaleX;
                        // var scaleAdjust = exIconScale / 1.55f;
                        var scale = obj->NameText->AtkResNode.ScaleX * state.ResNode->ScaleX * 2 * state.AdditionalCollisionScale;
                        // Service.Log.Info($"exIconScale {exIconScale} scaleAdjust {scaleAdjust}");

                        var colRes = &obj->CollisionNode1->AtkResNode;
                        colRes->OriginX = colRes->Width / 2f;
                        colRes->OriginY = colRes->Height;
                        colRes->SetScale(scale, scale);
                        state.IsCollisionFixed = true;
                    }
                }
            }

            // Debug icon padding by changing scale each frame
            // var scale = Service.Framework.LastUpdateUTC.Millisecond % 3000 / 500f * 4 + 1;
            // state.ExIconNode->AtkResNode.SetScale(scale, scale);
            // state.SubIconNode->AtkResNode.SetScale(scale, scale);
        }

        _namePlateDrawHook.Original(addon);
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
    }

    private static void ResetAllNodes()
    {
        foreach (var state in _stateCache) {
            ResetNodes(state);
        }
    }

    private static unsafe void ResetNodes(PlateState state)
    {
        if (state.IsGlobalScaleModified) {
            state.ResNode->OriginX = 0;
            state.ResNode->OriginY = 0;
            state.ResNode->SetScale(1f, 1f);
        }

        state.ExIconNode->AtkResNode.ToggleVisibility(false);
        state.SubIconNode->AtkResNode.ToggleVisibility(false);
        state.NamePlateObject->NameText->AtkResNode.SetScale(0.5f, 0.5f);

        // TODO needed?
        state.NamePlateObject->CollisionNode1->AtkResNode.SetScale(1f, 1f);
        state.NamePlateObject->CollisionNode1->AtkResNode.OriginX = 0;
        state.NamePlateObject->CollisionNode1->AtkResNode.OriginY = 0;

        state.IsModified = false;
    }

    private static unsafe AtkImageNode* CreateImageNode(uint nodeId, AtkComponentNode* parent, uint targetNodeId)
    {
        var imageNode = UiHelper.MakeImageNode(nodeId, new UiHelper.PartInfo(0, 0, 32, 32));
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

        var targetNode = UiHelper.GetNodeByID<AtkResNode>(&parent->Component->UldManager, targetNodeId);
        if (targetNode == null) {
            throw new Exception($"Failed to find link target ({targetNodeId}) for image node {nodeId}");
        }

        UiHelper.LinkNodeAfterTargetNode((AtkResNode*)imageNode, parent, targetNode);

        return imageNode;
    }

    private static unsafe void DestroyNodes(AddonNamePlate* addon)
    {
        Service.Log.Error("DestroyNodes");

        _addonNamePlateAddr = 0;

        var arr = addon->NamePlateObjectArray;
        if (arr == null) return;

        for (var i = 0; i < AddonNamePlate.NumNamePlateObjects; i++) {
            var np = arr[i];
            var resNode = np.ResNode;
            if (resNode == null) continue;
            var parentNode = resNode->ParentNode;
            if (parentNode == null) continue;
            var parentComponentNode = parentNode->GetAsAtkComponentNode();
            if (parentComponentNode == null) continue;
            var parentComponentNodeComponent = parentComponentNode->Component;
            if (parentComponentNodeComponent == null) continue;

            var exNode =
                UiHelper.GetNodeByID<AtkImageNode>(&parentComponentNodeComponent->UldManager, ExNodeId, NodeType.Image);
            if (exNode != null) {
                UiHelper.UnlinkAndFreeImageNodeIndirect(exNode, parentComponentNodeComponent->UldManager);
            }

            var subNode = UiHelper.GetNodeByID<AtkImageNode>(&parentComponentNodeComponent->UldManager, SubNodeId,
                NodeType.Image);
            if (subNode != null) {
                UiHelper.UnlinkAndFreeImageNodeIndirect(subNode, parentComponentNodeComponent->UldManager);
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