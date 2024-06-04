using System;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.Interop;
using PartyIcons.Configuration;
using PartyIcons.Utils;

namespace PartyIcons.Runtime;

public sealed class PartyListHUDUpdater : IDisposable
{
    private bool _enabled;

    private readonly Settings _configuration;
    private readonly PartyListHUDView _view;
    private readonly RoleTracker _roleTracker;

    private bool _displayingRoles;
    private bool _previousInParty;

    private bool _updateQueued;

    public PartyListHUDUpdater(PartyListHUDView view, RoleTracker roleTracker, Settings configuration)
    {
        _view = view;
        _roleTracker = roleTracker;
        _configuration = configuration;
    }

    public void Enable()
    {
        Service.Framework.Update += OnUpdate;
        Service.ClientState.EnterPvP += OnEnterPvP;
        Service.ClientState.LeavePvP += OnLeavePvP;
        Service.ClientState.TerritoryChanged += OnTerritoryChanged;
        _configuration.OnSave += OnConfigurationSave;
        _roleTracker.OnAssignedRolesUpdated += OnAssignedRolesUpdated;
    }

    public void Dispose()
    {
        Service.Framework.Update -= OnUpdate;
        Service.ClientState.EnterPvP -= OnEnterPvP;
        Service.ClientState.LeavePvP -= OnLeavePvP;
        Service.ClientState.TerritoryChanged -= OnTerritoryChanged;
        _configuration.OnSave -= OnConfigurationSave;
        _roleTracker.OnAssignedRolesUpdated -= OnAssignedRolesUpdated;

        RevertHud();
    }

    private unsafe void RevertHud()
    {
        var addonPartyList = (AddonPartyList*) Service.GameGui.GetAddonByName("_PartyList");
        if (addonPartyList == null)
        {
            return;
        }

        for (var i = 0; i < 8; i++) {
            _view.RevertPartyMemberRoleByIndex(addonPartyList, i);
        }

        _displayingRoles = false;
    }

    public void EnableUpdates(bool value)
    {
        if (!value && _enabled) {
            RevertHud();
        }

        _enabled = value;
    }

    private void OnTerritoryChanged(ushort id)
    {
        Service.Log.Verbose("PartyListHUDUpdater Forcing update due to territory change");
        UpdateHud();
    }

    private void OnEnterPvP()
    {
        if (_displayingRoles) {
            Service.Log.Verbose("PartyListHUDUpdater: reverting party list due to entering a PvP zone");
            RevertHud();
        }
    }

    private void OnLeavePvP()
    {
        Service.Log.Verbose("PartyListHUDUpdater: updating party list due to leaving a PvP zone");
        UpdateHud();
    }

    private void OnConfigurationSave()
    {
        if (_displayingRoles) {
            Service.Log.Verbose("PartyListHUDUpdater: reverting party list before the update due to config change");
            RevertHud();
        }

        Service.Log.Verbose("PartyListHUDUpdater forcing update due to changes in the config");
        UpdateHud();
    }

    private void OnAssignedRolesUpdated()
    {
        Service.Log.Verbose("PartyListHUDUpdater forcing update due to assignments update");
        UpdateHud();
    }

    private void OnUpdate(IFramework framework)
    {
        var inParty = Service.PartyList.Length != 0 || _configuration.TestingMode;

        if (!inParty && _previousInParty) {
            Service.Log.Verbose("No longer in party/testing mode, reverting/updating party list HUD changes");
            RevertHud();
            UpdateHud();
        }

        _previousInParty = inParty;

        if (_updateQueued) {
            // Service.Log.Verbose("Running queued update");
            _updateQueued = false;
            UpdateHud();
        }
    }

    private unsafe void UpdateHud()
    {
        if (!_configuration.DisplayRoleInPartyList
            || !_enabled
            || Service.ClientState.IsPvP) {
            return;
        }

        var addonPartyList = (AddonPartyList*)Service.GameGui.GetAddonByName("_PartyList");
        if (addonPartyList == null) {
            return;
        }

        var agentHud = AgentHUD.Instance();
        if (agentHud->PartyMemberCount == 0) {
            _updateQueued = true;
            return;
        }

        // Service.Log.Verbose($"Updating party list HUD. members = {Service.PartyList.Length}");

        var roleSet = false;
        for (var i = 0; i < 8; i++) {
            var hudPartyMember = agentHud->PartyMemberListSpan.GetPointer(i);
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

        _displayingRoles = roleSet;
    }

    private static unsafe uint GetWorldId(HudPartyMember* hudPartyMember)
    {
        var bc = hudPartyMember->Object;
        if (bc != null) {
            return bc->Character.HomeWorld;
        }

        if (hudPartyMember->ContentId > 0) {
            foreach (var partyMember in Service.PartyList) {
                if (hudPartyMember->ContentId == (ulong)partyMember.ContentId) {
                    return partyMember.World.Id;
                }
            }
        }

        return 65535;
    }

    public static unsafe void DebugPartyData()
    {
        Service.Log.Info("======");

        var agentHud = AgentHUD.Instance();
        Service.Log.Info($"Members (AgentHud) [{agentHud->PartyMemberCount}]:");
        for (var i = 0; i < agentHud->PartyMemberListSpan.Length; i++) {
            var hudPartyMember = agentHud->PartyMemberListSpan[i];
            if (hudPartyMember.Name != null) {
                var name = MemoryHelper.ReadSeStringNullTerminated((nint)hudPartyMember.Name);
                Service.Log.Info(
                    $"  [{i}] {name} -> 0x{(nint)hudPartyMember.Object:X} ({(hudPartyMember.Object != null ? hudPartyMember.Object->Character.HomeWorld : "?")}) {hudPartyMember.ContentId} {hudPartyMember.ObjectId}");
            }
        }

        Service.Log.Info($"Members (PartyList) [{Service.PartyList.Length}]:");
        for (var i = 0; i < Service.PartyList.Length; i++) {
            var member = Service.PartyList[i];
            Service.Log.Info($"  [{i}] {member?.Name.TextValue ?? "?"} ({member?.World.Id}) {member?.ContentId}");
        }

        var proxy = InfoProxyParty.Instance();
        var list = proxy->InfoProxyCommonList;
        Service.Log.Info($"Members (Proxy) [{list.CharDataSpan.Length}]:");
        for (var i = 0; i < list.CharDataSpan.Length; i++) {
            var data = list.CharDataSpan[i];
            var name = MemoryHelper.ReadSeStringNullTerminated((nint)data.Name);
            Service.Log.Info($"  [{i}] {name} ({data.HomeWorld}) {data.ContentId}");
        }
    }
}