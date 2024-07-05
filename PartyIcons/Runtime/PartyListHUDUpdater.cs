using System;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.Interop;
using PartyIcons.Configuration;
using PartyIcons.View;

namespace PartyIcons.Runtime;

public sealed class PartyListHUDUpdater : IDisposable
{
    private readonly Settings _configuration;
    private readonly PartyListHUDView _view;
    private readonly RoleTracker _roleTracker;
    private readonly PartyStateTracker _partyStateTracker;

    private bool _enabled;
    private bool _updateQueued;
    private bool _hasModifiedNodes;

    public PartyListHUDUpdater(PartyListHUDView view, RoleTracker roleTracker, Settings configuration,
        PartyStateTracker partyStateTracker)
    {
        _view = view;
        _roleTracker = roleTracker;
        _configuration = configuration;
        _partyStateTracker = partyStateTracker;
    }

    public void Enable()
    {
        Service.ClientState.EnterPvP += OnEnterPvP;
        Service.ClientState.LeavePvP += OnLeavePvP;
        Service.ClientState.TerritoryChanged += OnTerritoryChanged;
        _configuration.OnSave += OnConfigurationSave;
        _roleTracker.OnAssignedRolesUpdated += OnAssignedRolesUpdated;
        _partyStateTracker.OnPartyStateChange += OnPartyStateChange;
    }

    public void Dispose()
    {
        Service.ClientState.EnterPvP -= OnEnterPvP;
        Service.ClientState.LeavePvP -= OnLeavePvP;
        Service.ClientState.TerritoryChanged -= OnTerritoryChanged;
        _configuration.OnSave -= OnConfigurationSave;
        _roleTracker.OnAssignedRolesUpdated -= OnAssignedRolesUpdated;
        _partyStateTracker.OnPartyStateChange -= OnPartyStateChange;

        RevertHud();
    }

    public void EnableUpdates(bool value)
    {
        // Service.Log.Warning($"PartyListHUDUpdater: EnableUpdates({value})");
        if (!value && _enabled && _hasModifiedNodes) {
            Service.Log.Verbose("PartyListHUDUpdater: Reverting due to updates being disabled");
            RevertHud();
        }

        if (value != _enabled) {
            Service.Log.Verbose("PartyListHUDUpdater: Updating due to updates being enabled");
            _enabled = value;
            UpdateHud();
        }
    }

    private void OnTerritoryChanged(ushort id)
    {
        Service.Log.Verbose("PartyListHUDUpdater: Forcing update due to territory change");
        UpdateHud();
    }

    private void OnEnterPvP()
    {
        Service.Log.Verbose("PartyListHUDUpdater: Reverting party list due to entering a PvP zone");
        UpdateHud();
    }

    private void OnLeavePvP()
    {
        Service.Log.Verbose("PartyListHUDUpdater: Updating party list due to leaving a PvP zone");
        UpdateHud();
    }

    private void OnConfigurationSave()
    {
        Service.Log.Verbose("PartyListHUDUpdater: Forcing update due to changes in the config");
        UpdateHud();
    }

    private void OnAssignedRolesUpdated()
    {
        Service.Log.Verbose("PartyListHUDUpdater: Forcing update due to assignments update");
        UpdateHud();
    }

    private void OnPartyStateChange(PartyChangeType type)
    {
        if (type == PartyChangeType.Order) {
            Service.Log.Verbose($"PartyListHUDUpdater: Forcing update due to party state change ({type})");
            UpdateHud();
        }
    }

    private void RetryUpdate(IFramework framework)
    {
        UpdateHud();
    }

    private unsafe void UpdateHud()
    {
        if (!_configuration.DisplayRoleInPartyList || !_enabled || Service.ClientState.IsPvP) {
            if (_hasModifiedNodes) {
                Service.Log.Verbose("PartyListHUDUpdater: No longer displaying roles, reverting HUD changes");
                RevertHud();
            }

            return;
        }

        var agentHud = AgentHUD.Instance();
        if (agentHud->PartyMemberCount == 0) {
            if (!_updateQueued) {
                Service.Log.Verbose("PartyListHUDUpdater: Update queued");
                Service.Framework.Update += RetryUpdate;
                _updateQueued = true;
            }

            return;
        }

        if (_updateQueued) {
            Service.Log.Verbose("PartyListHUDUpdater: Update succeeded");
            Service.Framework.Update -= RetryUpdate;
            _updateQueued = false;
        }

        var addonPartyList = (AddonPartyList*)Service.GameGui.GetAddonByName("_PartyList");
        if (addonPartyList == null) {
            Service.Log.Warning("PartyListHUDUpdater: PartyList addon not visible during HUD update");
            return;
        }

        var roleSet = false;
        for (var i = 0; i < 8; i++) {
            var hudPartyMember = agentHud->PartyMembers.GetPointer(i);
            if (hudPartyMember->ContentId > 0) {
                var name = MemoryHelper.ReadStringNullTerminated((nint)hudPartyMember->Name);
                var worldId = GetWorldId(hudPartyMember);
                var hasRole = _roleTracker.TryGetAssignedRole(name, worldId, out var roleId);
                if (hasRole) {
                    // Service.Log.Info($"agentHud modify {i}");
                    _view.SetPartyMemberRoleByIndex(addonPartyList, i, roleId);
                    roleSet = true;
                    continue;
                }
            }

            // Service.Log.Info($"agentHud revert {i}");
            _view.RevertPartyMemberRoleByIndex(addonPartyList, i);
        }

        _hasModifiedNodes = roleSet;
    }

    private unsafe void RevertHud()
    {
        var addonPartyList = (AddonPartyList*)Service.GameGui.GetAddonByName("_PartyList");
        if (addonPartyList == null) return;

        for (var i = 0; i < 8; i++) {
            _view.RevertPartyMemberRoleByIndex(addonPartyList, i);
        }

        _hasModifiedNodes = false;
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
            if (member->Name != null) {
                var name = MemoryHelper.ReadSeStringNullTerminated((nint)member->Name);
                Service.Log.Info(
                    $"  [{i}] {name} -> 0x{(nint)member->Object:X} ({(member->Object != null ? member->Object->Character.HomeWorld : "?")}) {member->ContentId} {member->EntityId:X}");
            }
        }

        Service.Log.Info($"Members (PartyList) [{Service.PartyList.Length}]:");
        for (var i = 0; i < Service.PartyList.Length; i++) {
            var member = Service.PartyList[i];
            Service.Log.Info(
                $"  [{i}] {member?.Name.TextValue ?? "?"} ({member?.World.Id}) {member?.ContentId} [job={member?.ClassJob.Id}]");
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