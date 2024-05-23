using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Dalamud.Game.Config;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Data.Parsing;
using PartyIcons.Api;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.Utils;
using PartyIcons.View;
using SimpleTweaksPlugin.Utility;

namespace PartyIcons.Runtime;

public sealed class NameplateUpdater : IDisposable
{
    private readonly Settings _configuration;
    private readonly NameplateView _view;
    private readonly ViewModeSetter _modeSetter;

    [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 5C 24 ?? 45 38 BE", DetourName = nameof(SetNamePlateDetour))]
    private readonly Hook<SetNamePlateDelegate> _setNamePlateHook = null!;

    [Signature("0F B7 81 ?? ?? ?? ?? 4C 8B C1 66 C1 E0 06", DetourName = nameof(AddonNamePlateDrawDetour))]
    private readonly Hook<AddonNamePlateDrawDelegate> _namePlateDrawHook = null!;

    [Signature("E8 ?? ?? ?? ?? 48 8B 8F ?? ?? ?? ?? 0F 28 CE 48 8B 01", DetourName = nameof(AddonNameRefreshDetour))]
    private readonly Hook<AddonNameRefreshDelegate> _namePlateRefreshHook = null!;

    [Signature("48 8B C4 41 56 48 81 EC ?? ?? ?? ?? 48 89 58 F0", DetourName = nameof(SetEntityNameplateDetour))]
    private readonly Hook<SetEntityNameplateDelegate> _setEntityNameplateHook = null!;

    private unsafe delegate void AddonNamePlateDrawDelegate(AddonNamePlate* thisPtr);

    private delegate void AddonNameRefreshDelegate(nint ptr);

    private delegate void SetEntityNameplateDelegate(nint addon, nint p1, nint p2);

    public int DebugIcon { get; set; } = -1;

    private void AddonNameRefreshDetour(nint ptr)
    {
        // Service.Log.Info($"AddonNameRefreshDetour: 0x{ptr:X}");
        _namePlateRefreshHook.Original(ptr);
    }

    private void SetEntityNameplateDetour(nint addon, nint p1, nint p2)
    {
        // Service.Log.Info($"SetEntityNameplateDetour! addon[0x{addon:X}] p1[0x{p1:X}] p2[0x{p2:X}]");
        _setEntityNameplateHook.Original(addon, p1, p2);
    }

    public NameplateUpdater(Settings configuration, NameplateView view, ViewModeSetter modeSetter)
    {
        _configuration = configuration;
        _view = view;
        _modeSetter = modeSetter;

        Service.GameInteropProvider.InitializeFromAttributes(this);
    }

    public void Enable()
    {
        _setNamePlateHook.Enable();
        _namePlateDrawHook.Enable();
        _namePlateRefreshHook.Enable();
        _setEntityNameplateHook.Enable();

        Plugin.RoleTracker.OnAssignedRolesUpdated += ForceRedrawNamePlates;
    }

    public void Dispose()
    {
        _setNamePlateHook.Disable();
        _setNamePlateHook.Dispose();
        _namePlateDrawHook.Disable();
        _namePlateDrawHook.Dispose();
        _namePlateRefreshHook.Disable();
        _namePlateRefreshHook.Dispose();
        _setEntityNameplateHook.Disable();
        _setEntityNameplateHook.Dispose();

        Plugin.RoleTracker.OnAssignedRolesUpdated -= ForceRedrawNamePlates;

        unsafe {
            var addonPtr = (AddonNamePlate*)Service.GameGui.GetAddonByName("NamePlate");
            if (addonPtr != null) {
                DestroyNodes(addonPtr);
            }
        }
    }

    private delegate IntPtr SetNamePlateDelegate(IntPtr addon, bool isPrefixTitle, bool displayTitle, IntPtr title,
        IntPtr name, IntPtr fcName, IntPtr prefix, uint iconID);

    public IntPtr SetNamePlateDetour(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle,
        IntPtr title, IntPtr name, IntPtr fcName, IntPtr prefix, uint iconID)
    {
        var hookResult = IntPtr.MinValue;
        if (_addonNamePlateAddr != 0) {
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

        var npObject = new NamePlateObjectWrapper((AddonNamePlate.NamePlateObject*)namePlateObjectPtr);

        if (Service.ClientState.IsPvP
            || npObject is not { IsPlayer: true, NamePlateInfo: { ObjectID: not 0xE0000000 } npInfo }
            || npInfo.GetJobID() is < 1 or > JobConstants.MaxJob) {
            _view.SetupDefault(npObject);
            return;
        }

        var originalTitle = title;
        var originalName = name;
        var originalFcName = fcName;

        bool usedTextIcon;
        try {
            _view.NameplateDataForPC(npObject, ref isPrefixTitle, ref displayTitle, ref title, ref name, ref fcName, ref prefix,
                ref iconID, out usedTextIcon);
        }
        finally {
            if (originalName != name)
                SeStringUtils.FreePtr(name);
            if (originalTitle != title)
                SeStringUtils.FreePtr(title);
            if (originalFcName != fcName)
                SeStringUtils.FreePtr(fcName);
        }

        var status = npInfo.GetOnlineStatus();
        var isPriorityIcon = IsPriorityStatus(status);
        var statusIcon = StatusUtils.OnlineStatusToIconId(status);
        // if (isPriorityIcon && !usedTextIcon) {
        //     iconID = StatusUtils.OnlineStatusToIconId(status);
        // }

        // Replace -1 with empty texture dummy so icon is always rendered even for unselected targets (when unselected targets are hidden)
        if (iconID is EmptyIconId or 0) {
            iconID = placeholderEmptyIconId;
        }

        hookResult = _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName,
            prefix, iconID);

        var info = _plateInfoCache[_namePlateObjectPointerIndexMap[namePlateObjectPtr]];

        // info.ExIconNode->LoadIconTexture((int)_view.GetClassIcon(npObject.NamePlateInfo), 0);
        info.IsBlankIcon = iconID == placeholderEmptyIconId;

        // _view.PrepareNodeInline(info, npObject);
        _view.PrepareNodeLargeLeft(info, npObject);
        // _view


        // var resNode = npObject._pointer->ResNode;
        // var parentNode = resNode->ParentNode->GetAsAtkComponentNode();

        // var componentUldManager = &parentNode->Component->UldManager;
        // var imageNode = GetNodeByID<AtkComponentNode>(componentUldManager, 4);

        // var exNode = GetNodeByID<AtkImageNode>(componentUldManager, exNodeId);

        // PluginLog.Information(
        //     $"ResNode: 0x{(nint)resNode:X} Parent: 0x{(nint)parentNode:X} ImageNode: 0x{(nint)imageNode:X} ExNode: 0x{(nint)exNode:X}");
        // if (exNode == null) {
        //     PluginLog.Information($"  MakeInventoryNode: {exNodeId}");
        //     MakeExIconNode(exNodeId, parentNode);
        // }

        // var p = npObject._pointer;
        // npObject.MoveIconPosition(-80, 0);
        // PluginLog.Information($"{p->NameText->NodeText.ToString().Replace('\n', '|')} TextWH: {p->TextW}/{p->TextH} IconXY: {p->IconXAdjust}/{p->IconYAdjust}");

        _view.SetupForPC(info, npObject, isPriorityIcon);
        // AdjustExNode(*npObject._pointer);
    }

    // Unknown2 (2) shares the same internal 'renderer' as 1 & 5.
    // FriendlyCombatant (4) is any friendly NPC with a level in its nameplate. NPCs of type 1 can become type 4 when a FATE triggers for example (e.g. Boughbury Trader -> Lv32 Boughbury Trader). Shares same internal 'renderer' as 3
    // 6-8 are totally unknown
    public enum NamePlateKind : byte
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

    public const uint EmptyIconId = 4294967295;
    private const int placeholderEmptyIconId = 61696;
    private const int nameCollisionNodeId = 11;
    private const int nameTextNodeId = 3;
    private const int iconNodeId = 4;
    const int exNodeId = 1004;
    private static nint _addonNamePlateAddr;

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

            var exNode = UiHelper.GetNodeByID<AtkImageNode>(&parentComponentNodeComponent->UldManager, exNodeId);
            if (exNode != null) {
                UiHelper.UnlinkAndFreeImageNodeIndirect(exNode, parentComponentNodeComponent->UldManager);
            }
        }
    }

    private static PlateInfo[] _plateInfoCache = new PlateInfo[AddonNamePlate.NumNamePlateObjects];

    private static FrozenDictionary<nint, int> _namePlateObjectPointerIndexMap = new Dictionary<nint, int>().ToFrozenDictionary();

    public unsafe class PlateInfo
    {
        public AddonNamePlate.NamePlateObject* NamePlateObject;
        public AtkComponentNode* ComponentNode;
        public AtkResNode* ResNode;
        public AtkTextNode* NameTextNode;
        public AtkCollisionNode* NameCollisionNode;
        public AtkImageNode* IconNode;
        public AtkImageNode* ExIconNode;
        public PlateState State;
        public bool UseExIcon;
        public bool Visible;
        public bool IsBlankIcon;

        public override string ToString()
        {
            return $"{nameof(NamePlateObject)}: 0x{(nint)NamePlateObject:X}, " +
                   // $"{nameof(ComponentNode)}: 0x{(nint)ComponentNode:X}, " +
                   // $"{nameof(ResNode)}: 0x{(nint)ResNode:X}, " +
                   // $"{nameof(NameTextNode)}: 0x{(nint)NameTextNode:X}, " +
                   // $"{nameof(NameCollisionNode)}: 0x{(nint)NameCollisionNode:X}, " +
                   // $"{nameof(IconNode)}: 0x{(nint)IconNode:X}, " +
                   $"{nameof(ExIconNode)}: 0x{(nint)ExIconNode:X} " +
                   $"{nameof(State)}: {State}";
        }
    }

    public enum PlateState
    {
        Default,
        Player,
    }

    private static unsafe void CreateNodes(AddonNamePlate* addon)
    {
        Service.Log.Error("CreateNodes");

        var infoArray = new PlateInfo[AddonNamePlate.NumNamePlateObjects];
        var indexMap = new Dictionary<nint, int>();

        var arr = addon->NamePlateObjectArray;
        if (arr == null) return;

        for (var i = 0; i < AddonNamePlate.NumNamePlateObjects; i++) {
            var np = arr[i];
            var resNode = np.ResNode;
            var componentNode = resNode->ParentNode->GetAsAtkComponentNode();

            var textNode = UiHelper.GetNodeByID<AtkTextNode>(&componentNode->Component->UldManager, nameTextNodeId);
            var collisionNode = UiHelper.GetNodeByID<AtkCollisionNode>(&componentNode->Component->UldManager, nameCollisionNodeId);
            var iconNode = UiHelper.GetNodeByID<AtkImageNode>(&componentNode->Component->UldManager, iconNodeId);

            var exNode = UiHelper.GetNodeByID<AtkImageNode>(&componentNode->Component->UldManager, exNodeId);
            if (exNode == null) {
                exNode = MakeExIconNode(exNodeId, componentNode);
            }

            var namePlateObjectPointer = arr + i;

            infoArray[i] = new PlateInfo
            {
                NamePlateObject = namePlateObjectPointer,
                ComponentNode = componentNode,
                ResNode = resNode,
                NameTextNode = textNode,
                NameCollisionNode = collisionNode,
                IconNode = iconNode,
                ExIconNode = exNode,
                State = PlateState.Default,
                UseExIcon = false,
                Visible = false,
                IsBlankIcon = false,
            };
            indexMap[(nint)namePlateObjectPointer] = i;
        }

        for (var i = 0; i < infoArray.Length; i++) {
            var info = infoArray[i];
            if (info.ComponentNode->AtkResNode.IsVisible) {
                Service.Log.Info(
                    $"  {i} -> {(info.ComponentNode->AtkResNode.IsVisible ? 'v' : 'i')} {info} ({info.NamePlateObject->NameText->NodeText.ToString().Replace("\n", "\\n")} / {info.NamePlateObject->NameplateKind})");
            }
        }

        _plateInfoCache = infoArray;
        _namePlateObjectPointerIndexMap = indexMap.ToFrozenDictionary();
        _addonNamePlateAddr = (nint)addon;
    }

    private unsafe void AddonNamePlateDrawDetour(AddonNamePlate* addon)
    {
        if (addon->NamePlateObjectArray == null) return;

        if (_addonNamePlateAddr == 0) {
            CreateNodes(addon);
            Service.Framework.RunOnFrameworkThread(ForceRedrawNamePlates);
        }
        else {
            if ((nint)addon != _addonNamePlateAddr) {
                // DestroyNodes(addon); // TODO: is this really needed? we should destroy on addonfinalize or plugin dispose, i think
            }
        }

        var isPvP = Service.ClientState.IsPvP;

        foreach (var info in _plateInfoCache) {
            // var visible = (info.NameTextNode->AtkResNode.NodeFlags & NodeFlags.Visible) != 0;
            // var appeared = visible && !info.Visible;
            // info.Visible = visible;

            // Service.Log.Info($" {info.NamePlateObject->IsVisible ? 'v' : 'i'} {(info.ComponentNode->AtkResNode.IsVisible ? 'v' : 'i')} {info} ({info.NamePlateObject->NameText->NodeText.ToString().Replace("\n", "\\n")} / {(NamePlateKind)info.NamePlateObject->NameplateKind} / {info.State})");
            if (info.State == PlateState.Player) {
                var kind = (NamePlateKind)info.NamePlateObject->NameplateKind;
                if (kind != NamePlateKind.Player || (info.ResNode->NodeFlags & NodeFlags.Visible) == 0 || isPvP) {
                    ResetNode(info);
                }
                else if (info.UseExIcon) {
                    // if (appeared) {
                    //     FixNodePosition(info);
                    // }
                    FixNodeVisibility(info);
                }
            }
            // DebugNode(info);

            // info.Visible = visible;
        }

        _namePlateDrawHook.Original(addon);
    }

    private static unsafe void DebugNode(PlateInfo info)
    {
        var scale = Service.Framework.LastUpdateUTC.Millisecond % 3000 / 500f * 4 + 1;
        info.ExIconNode->AtkResNode.SetScale(scale, scale);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void FixNodeVisibility(PlateInfo info)
    {
        info.ExIconNode->AtkResNode.NodeFlags ^=
            (info.ExIconNode->AtkResNode.NodeFlags ^ info.NameTextNode->AtkResNode.NodeFlags) &
            (NodeFlags.UseDepthBasedPriority | NodeFlags.Visible);
    }

    private static unsafe void ResetNode(PlateInfo info)
    {
        info.ExIconNode->AtkResNode.ToggleVisibility(false);
        info.NamePlateObject->NameText->AtkResNode.SetScale(0.5f, 0.5f);
        info.State = PlateState.Default;
    }

    private static unsafe AtkImageNode* MakeExIconNode(uint nodeId, AtkComponentNode* parent)
    {
        var imageNode = UiHelper.MakeImageNode(nodeId, new UiHelper.PartInfo(0, 0, 32, 32));
        imageNode->AtkResNode.NodeFlags = NodeFlags.AnchorBottom | NodeFlags.AnchorRight | NodeFlags.Enabled |
                                          NodeFlags.EmitsEvents | NodeFlags.UseDepthBasedPriority;
        imageNode->AtkResNode.SetWidth(32);
        imageNode->AtkResNode.SetHeight(32);
        // imageNode->AtkResNode.SetScale(1f, 1f);
        // imageNode->AtkResNode.SetPositionShort(0, 0); // will be updated later
        // imageNode->AtkResNode.ToggleVisibility(false);

        imageNode->WrapMode = 1;
        imageNode->Flags = (byte)ImageNodeFlags.AutoFit;
        imageNode->LoadIconTexture(60071, 0);

        var targetNode = UiHelper.GetNodeByID<AtkResNode>(&parent->Component->UldManager, 4);
        UiHelper.LinkNodeAfterTargetNode((AtkResNode*)imageNode, parent, targetNode);

        return imageNode;
    }

    private bool IsPriorityStatus(Status status)
    {
        if (_configuration.UsePriorityIcons == false && status != Status.Disconnected)
            return false;

        if (_modeSetter.ZoneType == ZoneType.Foray)
            return StatusUtils.PriorityStatusesInForay.Contains(status);

        if (_modeSetter.InDuty)
            return StatusUtils.PriorityStatusesInDuty.Contains(status);

        return status != Status.None;
    }

    public static unsafe void ForceRedrawNamePlates()
    {
        Service.Log.Info("ForceRedrawNamePlates");
        var addon = (AddonNamePlate*)Service.GameGui.GetAddonByName("NamePlate");
        if (addon != null) {
            var setting = UiConfigOption.NamePlateDispJobIconType.ToString();
            var value = Service.GameConfig.UiConfig.GetUInt(setting);
            Service.GameConfig.UiConfig.Set(setting, value == 1u ? 0u : 1u);
            Service.GameConfig.UiConfig.Set(setting, value);
            addon->DoFullUpdate = 1;
        }
    }
}