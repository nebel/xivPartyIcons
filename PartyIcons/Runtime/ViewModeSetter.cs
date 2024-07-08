using System;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using PartyIcons.Configuration;
using PartyIcons.View;

namespace PartyIcons.Runtime;

public enum ZoneType
{
    Overworld,
    Dungeon,
    Raid,
    AllianceRaid,
    FieldOperation,
}

public sealed class ViewModeSetter
{
    private ZoneType ZoneType { get; set; } = ZoneType.Overworld;

    private readonly NameplateView _nameplateView;
    private readonly Settings _configuration;
    private readonly ChatNameUpdater _chatNameUpdater;
    private readonly PartyListHUDUpdater _partyListHudUpdater;

    private ExcelSheet<ContentFinderCondition> _contentFinderConditionsSheet = null!;

    public ViewModeSetter(NameplateView nameplateView, Settings configuration, ChatNameUpdater chatNameUpdater,
        PartyListHUDUpdater partyListHudUpdater)
    {
        _nameplateView = nameplateView;
        _configuration = configuration;
        _chatNameUpdater = chatNameUpdater;
        _partyListHudUpdater = partyListHudUpdater;
    }

    private void OnConfigurationSave()
    {
        ForceRefresh();
    }

    public void Enable()
    {
        _contentFinderConditionsSheet = Service.DataManager.GameData.GetExcelSheet<ContentFinderCondition>() ??
                                        throw new InvalidOperationException();
        _configuration.OnSave += OnConfigurationSave;
        Service.ClientState.TerritoryChanged += OnTerritoryChanged;

        _chatNameUpdater.OthersMode = _configuration.ChatOthers;

        ForceRefresh();
    }

    public void ForceRefresh()
    {
        OnTerritoryChanged(0);
    }

    public void Disable()
    {
        Service.ClientState.TerritoryChanged -= OnTerritoryChanged;
    }

    public void Dispose()
    {
        _configuration.OnSave -= OnConfigurationSave;
        Disable();
    }

    private void OnTerritoryChanged(ushort e)
    {
        var content =
            _contentFinderConditionsSheet.FirstOrDefault(t => t.TerritoryType.Row == Service.ClientState.TerritoryType);

        if (content == null) {
            Service.Log.Verbose($"Content null {Service.ClientState.TerritoryType}");

            ZoneType = ZoneType.Overworld;
            _chatNameUpdater.PartyMode = _configuration.ChatOverworld;
            _nameplateView.SetZoneType(ZoneType);
        }
        else {
            if (_configuration.ChatContentMessage) {
                Service.ChatGui.Print($"Entering {content.Name}.", Service.PluginInterface.InternalName, 45);
            }

            var memberType = content.ContentMemberType.Row;

            if (content.TerritoryType.Value is { TerritoryIntendedUse: 41 or 48 }) {
                // Bozja/Eureka
                memberType = 127;
            }

            ZoneType = memberType switch
            {
                2 => ZoneType.Dungeon,
                3 => ZoneType.Raid,
                4 => ZoneType.AllianceRaid,
                127 => ZoneType.FieldOperation,
                _ => ZoneType.Dungeon
            };

            Service.Log.Debug(
                $"Territory changed {content.Name} (id {content.RowId} type {content.ContentType.Row}, terr {Service.ClientState.TerritoryType}, iu {content.TerritoryType.Value?.TerritoryIntendedUse}, memtype {content.ContentMemberType.Row}, overriden {memberType}, zoneType {ZoneType})");
        }

        _chatNameUpdater.PartyMode = ZoneType switch
        {
            ZoneType.Overworld => _configuration.ChatOverworld,
            ZoneType.Dungeon => _configuration.ChatDungeon,
            ZoneType.Raid => _configuration.ChatRaid,
            ZoneType.AllianceRaid => _configuration.ChatAllianceRaid,
            ZoneType.FieldOperation => _configuration.ChatOverworld,
            _ => _configuration.ChatDungeon
        };

        _nameplateView.SetZoneType(ZoneType);

        var enableHud =
            _nameplateView.PartyDisplay.Mode is NameplateMode.RoleLetters or NameplateMode.SmallJobIconAndRole &&
            _nameplateView.PartyDisplay.RoleDisplayStyle == RoleDisplayStyle.Role;
        _partyListHudUpdater.EnableUpdates(enableHud);

        Service.Log.Verbose($"Setting modes: nameplates party {_nameplateView.PartyDisplay.Mode} others {_nameplateView.OthersDisplay.Mode}, chat {_chatNameUpdater.PartyMode}, update HUD {enableHud}");
        Service.Log.Debug($"Entered ZoneType {ZoneType.ToString()}");

        Service.Framework.RunOnFrameworkThread(NameplateUpdater.ForceRedrawNamePlates);
    }
}