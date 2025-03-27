using Dalamud.Hooking;
using System;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using PartyIcons.Configuration;

namespace PartyIcons.Runtime;

public sealed class PartyListHUDUpdater : IDisposable
{
    private readonly Settings _configuration;
    private readonly RoleTracker _roleTracker;

    private bool _enabled;
    private bool _isShowingRoles;
    private bool _hasModifiedNodes;

    private readonly bool[] _occupiedSlots = new bool[8];
    private readonly string[] _originalText = new string[8];

    private const int NumberStructStartIndex = 6;
    private const int NumberStructSize = 23;

    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 54 41 55 41 56 41 57 48 83 EC 30 4C 8B 6A 20", DetourName = nameof(OnRequestedUpdateDetour))]
    private readonly Hook<OnRequestedUpdateDelegate> _onRequestedUpdateHook = null!;

    private unsafe delegate void OnRequestedUpdateDelegate(AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);

    private unsafe void OnRequestedUpdateDetour(
        AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        // Service.Log.Warning("PartyListHUDUpdater: OnRequestedUpdateDetour");

        if (!_enabled) {
            _onRequestedUpdateHook.Original.Invoke(addon, numberArrayData, stringArrayData);
            return;
        }

        var agentHud = AgentHUD.Instance();
        if (agentHud->PartyMemberCount == 0) {
            _onRequestedUpdateHook.Original.Invoke(addon, numberArrayData, stringArrayData);
            return;
        }

        var stringData = stringArrayData[(int)StringArrayType.PartyList];
        var array = stringData->StringArray;

        _occupiedSlots.AsSpan().Clear();

        for (var i = 0; i < 8; i++) {
            var hudPartyMember = agentHud->PartyMembers.GetPointer(i);
            if (hudPartyMember->ContentId > 0) {
                var name = hudPartyMember->Name;
                var worldId = GetWorldId(hudPartyMember);
                var hasRole = _roleTracker.TryGetAssignedRole(name, worldId, out var roleId);
                if (hasRole) {
                    var index = hudPartyMember->Index;
                    var arrayIndex = NumberStructStartIndex + NumberStructSize * index;
                    _occupiedSlots[index] = true;
                    _originalText[index] = MemoryHelper.ReadStringNullTerminated((nint)array[arrayIndex]);
                    stringData->SetValue(arrayIndex, Plugin.PlayerStylesheet.GetRolePlate(roleId).Encode(), false, true, true);
                }
            }
        }

        _onRequestedUpdateHook.Original.Invoke(addon, numberArrayData, stringArrayData);

        for (var i = 0; i < 8; i++) {
            if (_occupiedSlots[i]) {
                var arrayIndex = NumberStructStartIndex + NumberStructSize * i;
                stringData->SetValue(arrayIndex, _originalText[i], false, true, true);
            }
        }
    }

    public PartyListHUDUpdater(RoleTracker roleTracker, Settings configuration)
    {
        _roleTracker = roleTracker;
        _configuration = configuration;

        Service.GameInteropProvider.InitializeFromAttributes(this);
    }

    public void Enable()
    {
        Service.ClientState.EnterPvP += OnEnterPvP;
        Service.ClientState.LeavePvP += OnLeavePvP;
        _configuration.OnSave += OnConfigurationSave;
        _roleTracker.OnAssignedRolesUpdated += OnAssignedRolesUpdated;
    }

    public void Dispose()
    {
        Service.ClientState.EnterPvP -= OnEnterPvP;
        Service.ClientState.LeavePvP -= OnLeavePvP;
        _configuration.OnSave -= OnConfigurationSave;
        _roleTracker.OnAssignedRolesUpdated -= OnAssignedRolesUpdated;

        _onRequestedUpdateHook.Dispose();

        RevertNodes();
        ForceArrayUpdate();
    }

    private void CheckState()
    {
        var shouldEnable = _isShowingRoles && _configuration.DisplayRoleInPartyList && !Service.ClientState.IsPvP;
        Service.Log.Warning($"{_isShowingRoles} {_configuration.DisplayRoleInPartyList} && {!Service.ClientState.IsPvP}");
        if (_enabled != shouldEnable) {
            if (!shouldEnable) {
                Service.Log.Verbose("PartyListHUDUpdater: Disable");
                RevertNodes();
                _onRequestedUpdateHook.Disable();
            }
            else {
                Service.Log.Verbose("PartyListHUDUpdater: Enable");
                ModifyNodes();
                _onRequestedUpdateHook.Enable();
            }
            _enabled = shouldEnable;
            ForceArrayUpdate();
        }
    }

    public void SetRoleVisibility(bool value)
    {
        Service.Log.Warning("PartyListHUDUpdater: SetRoleVisibility");
        _isShowingRoles = value;
        CheckState();
    }

    private void OnEnterPvP()
    {
        Service.Log.Verbose("PartyListHUDUpdater: OnEnterPvP");
        CheckState();
    }

    private void OnLeavePvP()
    {
        Service.Log.Verbose("PartyListHUDUpdater: OnLeavePvP");
        CheckState();
    }

    private void OnConfigurationSave()
    {
        Service.Log.Verbose("PartyListHUDUpdater: OnConfigurationSave");
        CheckState();
    }

    private void OnAssignedRolesUpdated()
    {
        Service.Log.Verbose("PartyListHUDUpdater: OnAssignedRolesUpdated");
        ForceArrayUpdate();
    }

    private unsafe void ModifyNodes()
    {
        if (!_hasModifiedNodes) {
            var addonPartyList = (AddonPartyList*)Service.GameGui.GetAddonByName("_PartyList");
            foreach (ref var member in addonPartyList->PartyMembers) {
                member.Name->SetPositionShort(29, 0);
                member.GroupSlotIndicator->SetPositionShort(6, 0);
            }
            _hasModifiedNodes = true;
        }
    }

    private unsafe void RevertNodes()
    {
        if (_hasModifiedNodes) {
            var addonPartyList = (AddonPartyList*)Service.GameGui.GetAddonByName("_PartyList");
            foreach (ref var member in addonPartyList->PartyMembers) {
                member.Name->SetPositionShort(19, 0);
                member.GroupSlotIndicator->SetPositionShort(0, 0);
            }
            _hasModifiedNodes = false;
        }
    }

    private static unsafe void ForceArrayUpdate()
    {
        var arrayData = AtkStage.Instance()->GetStringArrayData(StringArrayType.PartyList);
        arrayData->UpdateState = 1;
    }

    private static unsafe uint GetWorldId(HudPartyMember* hudPartyMember)
    {
        var bc = hudPartyMember->Object;
        if (bc != null) {
            return bc->Character.HomeWorld;
        }

        if (hudPartyMember->ContentId > 0) {
            var gm = GroupManager.Instance();
            for (var i = 0; i < gm->MainGroup.MemberCount; i++) {
                var member = gm->MainGroup.PartyMembers.GetPointer(i);
                if (hudPartyMember->ContentId == member->ContentId) {
                    return member->HomeWorld;
                }
            }
        }

        return 65535;
    }

    public static unsafe void DebugPartyData()
    {
        Service.Log.Info("======");

        var agentHud = AgentHUD.Instance();
        Service.Log.Info($"Members (AgentHud) [{agentHud->PartyMemberCount}] (0x{(nint)agentHud:X}):");
        for (var i = 0; i < agentHud->PartyMembers.Length; i++) {
            var member = agentHud->PartyMembers.GetPointer(i);
            if (member->Name.HasValue) {
                Service.Log.Info(
                    $"  [{i}] {member->Name} -> 0x{(nint)member->Object:X} ({(member->Object != null ? member->Object->Character.HomeWorld : "?")}) {member->ContentId} {member->EntityId:X} (index={member->Index})");
            }
        }

        Service.Log.Info($"Members (PartyList) [{Service.PartyList.Length}]:");
        for (var i = 0; i < Service.PartyList.Length; i++) {
            var member = Service.PartyList[i];
            Service.Log.Info(
                $"  [{i}] {member?.Name.TextValue ?? "?"} ({member?.World.RowId}) {member?.ContentId} [job={member?.ClassJob.RowId}]");
        }

        var gm = GroupManager.Instance();
        Service.Log.Info($"Members (GroupManager) [{gm->MainGroup.MemberCount}] (0x{(nint)gm:X}):");
        for (var i = 0; i < gm->MainGroup.MemberCount; i++) {
            var member = gm->MainGroup.PartyMembers.GetPointer(i);
            if (member->HomeWorld != 65535) {
                Service.Log.Info(
                    $"  [{i}] {member->NameString} -> 0x{(nint)member->EntityId:X} ({member->HomeWorld}) {member->ContentId} [job={member->ClassJob}]");
            }
        }

        var proxy = InfoProxyPartyMember.Instance();
        var list = proxy->InfoProxyCommonList;
        Service.Log.Info($"Members (Proxy) [{list.CharDataSpan.Length}]:");
        for (var i = 0; i < list.CharDataSpan.Length; i++) {
            var data = list.CharDataSpan[i];
            Service.Log.Info($"  [{i}] {data.NameString} ({data.HomeWorld}) {data.ContentId} {data.Job}");
        }
    }
}