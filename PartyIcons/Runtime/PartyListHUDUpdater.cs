using System;
using System.Linq;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.Interop;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.Utils;

namespace PartyIcons.Runtime;

public sealed class PartyListHUDUpdater : IDisposable
{
    public bool UpdateHUD = false;

    private readonly Settings _configuration;
    private readonly PartyListHUDView _view;
    private readonly RoleTracker _roleTracker;

    private bool _displayingRoles;

    private bool _previousInParty;
    private bool _previousTesting;
    private DateTime _lastUpdate = DateTime.Today;

    private const string PrepareZoningSig = "48 89 5C 24 ?? 57 48 83 EC 40 F6 42 0D 08";
    private delegate nint PrepareZoningDelegate (nint a1, nint a2, byte a3);
    private Hook<PrepareZoningDelegate> prepareZoningHook;

    public PartyListHUDUpdater(PartyListHUDView view, RoleTracker roleTracker, Settings configuration)
    {
        _view = view;
        _roleTracker = roleTracker;
        _configuration = configuration;
        
        prepareZoningHook =
            Service.GameInteropProvider.HookFromAddress<PrepareZoningDelegate>(
                Service.SigScanner.ScanText(PrepareZoningSig), PrepareZoning);
        prepareZoningHook?.Enable();
    }

    public void Enable()
    {
        _roleTracker.OnAssignedRolesUpdated += OnAssignedRolesUpdated;
        Service.Framework.Update += OnUpdate;
        _configuration.OnSave += OnConfigurationSave;
        Service.ClientState.EnterPvP += OnEnterPvP;
    }

    public void Dispose()
    {
        Service.ClientState.EnterPvP -= OnEnterPvP;
        _configuration.OnSave -= OnConfigurationSave;
        Service.Framework.Update -= OnUpdate;
        _roleTracker.OnAssignedRolesUpdated -= OnAssignedRolesUpdated;
        prepareZoningHook?.Dispose();
    }

    private nint PrepareZoning(nint a1, nint a2, byte a3)
    {
        Service.Log.Verbose("PartyListHUDUpdater Forcing update due to zoning");
        // Service.Log.Verbose(_view.GetDebugInfo());
        UpdatePartyListHUD();
        return prepareZoningHook.OriginalDisposeSafe(a1,a2,a3);
    }

    private void OnEnterPvP()
    {
        if (_displayingRoles)
        {
            Service.Log.Verbose("PartyListHUDUpdater: reverting party list due to entering a PvP zone");
            _displayingRoles = false;
            _view.RevertSlotNumbers();
        }
    }

    private void OnConfigurationSave()
    {
        if (_displayingRoles)
        {
            Service.Log.Verbose("PartyListHUDUpdater: reverting party list before the update due to config change");
            _view.RevertSlotNumbers();
        }

        Service.Log.Verbose("PartyListHUDUpdater forcing update due to changes in the config");
        // Service.Log.Verbose(_view.GetDebugInfo());
        UpdatePartyListHUD();
    }

    private void OnAssignedRolesUpdated()
    {
        Service.Log.Verbose("PartyListHUDUpdater forcing update due to assignments update");
        // Service.Log.Verbose(_view.GetDebugInfo());
        UpdatePartyListHUD();
    }
    
    private void OnUpdate(IFramework framework)
    {
        var inParty = Service.PartyList.Any();

        if ((!inParty && _previousInParty) || (!_configuration.TestingMode && _previousTesting))
        {
            Service.Log.Verbose("No longer in party/testing mode, reverting party list HUD changes");
            _displayingRoles = false;
            _view.RevertSlotNumbers();
        }

        _previousInParty = inParty;
        _previousTesting = _configuration.TestingMode;

        if (DateTime.Now - _lastUpdate > TimeSpan.FromSeconds(15))
        {
            Service.Log.Verbose("PartyListHUDUpdater forcing update due to time");
            UpdatePartyListHUD();
            _lastUpdate = DateTime.Now;
        }
    }

    private unsafe void UpdatePartyListHUD()
    {
        if (!_configuration.DisplayRoleInPartyList)
        {
            return;
        }

        if (!UpdateHUD)
        {
            return;
        }

        if (Service.ClientState.IsPvP)
        {
            return;
        }

        // Service.Log.Verbose($"Updating party list HUD. members = {Service.PartyList.Length}");
        _displayingRoles = true;

        var addonPartyList = (AddonPartyList*) Service.GameGui.GetAddonByName("_PartyList");
        if (addonPartyList == null)
        {
            return;
        }

        var agentHud = AgentHUD.Instance();

        for (var i = 0; i < 8; i++) {
            var hudPartyMember = agentHud->PartyMemberListSpan.GetPointer(i);
            if (hudPartyMember->ContentId > 0) {
                var name = MemoryHelper.ReadStringNullTerminated((nint)hudPartyMember->Name);
                var worldId = GetWorldId(hudPartyMember);
                var hasRole = _roleTracker.TryGetAssignedRole(name, worldId, out var roleId);
                if (hasRole) {
                    // Service.Log.Info($"agentHud modify {i}");
                    _view.SetPartyMemberRoleByIndex(addonPartyList, i, roleId);
                    continue;
                }
            }

            // Service.Log.Info($"Reverting {i}");
            _view.RevertPartyMemberRoleByIndex(addonPartyList, i);
        }
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
}
