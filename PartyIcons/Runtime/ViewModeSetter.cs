using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using PartyIcons.Configuration;
using PartyIcons.Utils;
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
    /// <summary>
    /// Whether the player is currently in a duty.
    /// </summary>
    public bool InDuty => ZoneType != ZoneType.Overworld;
    
    public ZoneType ZoneType { get; private set; } = ZoneType.Overworld;

    private readonly NameplateView _nameplateView;
    private readonly Settings _configuration;
    private readonly ChatNameUpdater _chatNameUpdater;
    private readonly PartyListHUDUpdater _partyListHudUpdater;
    private readonly StatusResolver _statusResolver;

    private static readonly StatusSelector DefaultStatusSelector = new(ZoneType.Overworld);

    private ExcelSheet<ContentFinderCondition> _contentFinderConditionsSheet;

    public ViewModeSetter(NameplateView nameplateView, Settings configuration, ChatNameUpdater chatNameUpdater,
        PartyListHUDUpdater partyListHudUpdater, StatusResolver statusResolver)
    {
        _nameplateView = nameplateView;
        _configuration = configuration;
        _chatNameUpdater = chatNameUpdater;
        _partyListHudUpdater = partyListHudUpdater;
        _statusResolver = statusResolver;
        
        _configuration.OnSave += OnConfigurationSave;
    }

    private void OnConfigurationSave()
    {
        ForceRefresh();
    }

    public void Enable()
    {
        _contentFinderConditionsSheet = Service.DataManager.GameData.GetExcelSheet<ContentFinderCondition>() ?? throw new InvalidOperationException();

        ForceRefresh();
        Service.ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    public void ForceRefresh()
    {
        _nameplateView.OthersMode = _configuration.NameplateOthers;
        _chatNameUpdater.OthersMode = _configuration.ChatOthers;

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

    // public DisplayConfig GetDisplayConfig(DisplaySelector selector)
    // {
    //     var configs = _configuration.DisplayConfigs;
    //     switch (selector.Preset) {
    //         case DisplayPreset.Default:
    //             return configs.Default;
    //         case DisplayPreset.Hide:
    //             return configs.Hide;
    //         case DisplayPreset.SmallJobIcon:
    //             return configs.SmallJobIcon;
    //         case DisplayPreset.SmallJobIconAndRole:
    //             return configs.SmallJobIconAndRole;
    //         case DisplayPreset.BigJobIcon:
    //             return configs.BigJobIcon;
    //         case DisplayPreset.BigJobIconAndPartySlot:
    //             return configs.BigJobIconAndPartySlot;
    //         case DisplayPreset.RoleLetters:
    //             return configs.RoleLetters;
    //         case DisplayPreset.Custom:
    //             foreach (var config in configs.Custom.Where(config => config.Id == selector.Id)) {
    //                 return config;
    //             }
    //
    //             Service.Log.Warning($"Couldn't find custom preset with id {selector.Id}, falling back to default");
    //             return configs.Default;
    //         default:
    //             Service.Log.Warning($"Couldn't find preset of type {selector.Preset}, falling back to default");
    //             return configs.Default;
    //     }
    // }
    //
    // public StatusConfig GetStatusConfig(StatusSelector selector)
    // {
    //     var configs = _configuration.StatusConfigs;
    //     switch (selector.Preset) {
    //         case StatusPreset.Overworld:
    //             return configs.Overworld;
    //         case StatusPreset.Instances:
    //             return configs.Instances;
    //         case StatusPreset.FieldOperations:
    //             return configs.FieldOperations;
    //         case StatusPreset.Custom:
    //             foreach (var config in configs.Custom.Where(config => config.Id == selector.Id)) {
    //                 return config;
    //             }
    //
    //             Service.Log.Warning($"Couldn't find custom preset with id {selector.Id}, falling back to overworld");
    //             return configs.Overworld;
    //         default:
    //             Service.Log.Warning($"Couldn't find preset of type {selector.Preset}, falling back to overworld");
    //             return configs.Overworld;
    //     }
    // }

    private void SetNameplateViewZone(ZoneType zoneType)
    {
        var selectors = _configuration.DisplaySelectors;

        var partyDisplay = _configuration.GetDisplayConfig(zoneType switch
        {
            ZoneType.Overworld => selectors.DisplayOverworld,
            ZoneType.Dungeon => selectors.DisplayDungeon,
            ZoneType.Raid => selectors.DisplayRaid,
            ZoneType.AllianceRaid => selectors.DisplayAllianceRaid,
            ZoneType.FieldOperation => selectors.DisplayFieldOperationParty,
            _ => throw new ArgumentOutOfRangeException($"Unknown zone type {zoneType}")
        });

        var othersDisplay = _configuration.GetDisplayConfig(zoneType switch
        {
            ZoneType.Overworld => selectors.DisplayOthers,
            ZoneType.Dungeon => selectors.DisplayOthers,
            ZoneType.Raid => selectors.DisplayOthers,
            ZoneType.AllianceRaid => selectors.DisplayOthers,
            ZoneType.FieldOperation => selectors.DisplayFieldOperationOthers,
            _ => throw new ArgumentOutOfRangeException($"Unknown zone type {zoneType}")
        });

        _nameplateView.ZoneType = zoneType;

        _nameplateView.PartyDisplay = partyDisplay;
        if (!partyDisplay.StatusSelectors.TryGetValue(zoneType, out var partyStatusSelector)) {
            Service.Log.Warning($"Couldn't find status selector for zoneType {zoneType} in config {partyDisplay.Preset}/{partyDisplay.Id}");
            partyStatusSelector = DefaultStatusSelector;
        }
        _nameplateView.PartyStatus = StatusUtils.DictToArray(_configuration.GetStatusConfig(partyStatusSelector).DisplayMap);

        _nameplateView.OthersDisplay = othersDisplay;
        if (!othersDisplay.StatusSelectors.TryGetValue(zoneType, out var othersStatusSelector)) {
            Service.Log.Warning($"Couldn't find status selector for zoneType {zoneType} in config {othersDisplay.Preset}/{othersDisplay.Id}");
            othersStatusSelector = DefaultStatusSelector;
        }
        _nameplateView.OthersStatus = StatusUtils.DictToArray(_configuration.GetStatusConfig(othersStatusSelector).DisplayMap);
    }

    private void OnTerritoryChanged(ushort e)
    {
        var content =
            _contentFinderConditionsSheet.FirstOrDefault(t => t.TerritoryType.Row == Service.ClientState.TerritoryType);

        if (content == null)
        {
            Service.Log.Verbose($"Content null {Service.ClientState.TerritoryType}");
            ZoneType = ZoneType.Overworld;
            _nameplateView.PartyMode = _configuration.NameplateOverworld;
            _nameplateView.OthersMode = _configuration.NameplateOthers;
            _chatNameUpdater.PartyMode = _configuration.ChatOverworld;
            _statusResolver.SetZoneType(ZoneType.Overworld);
            SetNameplateViewZone(ZoneType);
        }
        else
        {
            if (_configuration.ChatContentMessage)
            {
                Service.ChatGui.Print($"Entering {content.Name}.");
            }

            var memberType = content.ContentMemberType.Row;

            if (content.TerritoryType.Value is { TerritoryIntendedUse: 41 or 48 } )
            {
                // Bozja/Eureka
                memberType = 127;
            }

            Service.Log.Debug(
                $"Territory changed {content.Name} (id {content.RowId} type {content.ContentType.Row}, terr {Service.ClientState.TerritoryType}, iu {content.TerritoryType.Value?.TerritoryIntendedUse}, memtype {content.ContentMemberType.Row}, overriden {memberType})");

            switch (memberType)
            {
                case 2:
                    ZoneType = ZoneType.Dungeon;
                    _nameplateView.PartyMode = _configuration.NameplateDungeon;
                    _nameplateView.OthersMode = _configuration.NameplateOthers;
                    _chatNameUpdater.PartyMode = _configuration.ChatDungeon;
                    _statusResolver.SetZoneType(ZoneType.Dungeon);
                    SetNameplateViewZone(ZoneType);

                    break;

                case 3:
                    ZoneType = ZoneType.Raid;
                    _nameplateView.PartyMode = _configuration.NameplateRaid;
                    _nameplateView.OthersMode = _configuration.NameplateOthers;
                    _chatNameUpdater.PartyMode = _configuration.ChatRaid;
                    _statusResolver.SetZoneType(ZoneType.Raid);
                    SetNameplateViewZone(ZoneType);

                    break;

                case 4:
                    ZoneType = ZoneType.AllianceRaid;
                    _nameplateView.PartyMode = _configuration.NameplateAllianceRaid;
                    _nameplateView.OthersMode = _configuration.NameplateOthers;
                    _chatNameUpdater.PartyMode = _configuration.ChatAllianceRaid;
                    _statusResolver.SetZoneType(ZoneType.AllianceRaid);
                    SetNameplateViewZone(ZoneType);

                    break;

                case 127:
                    ZoneType = ZoneType.FieldOperation;
                    _nameplateView.PartyMode = _configuration.NameplateBozjaParty;
                    _nameplateView.OthersMode = _configuration.NameplateBozjaOthers;
                    _chatNameUpdater.PartyMode = _configuration.ChatOverworld;
                    _statusResolver.SetZoneType(ZoneType.FieldOperation);
                    SetNameplateViewZone(ZoneType);

                    break;

                default:
                    ZoneType = ZoneType.Dungeon;
                    _nameplateView.PartyMode = _configuration.NameplateDungeon;
                    _nameplateView.OthersMode = _configuration.NameplateOthers;
                    _chatNameUpdater.PartyMode = _configuration.ChatDungeon;
                    _statusResolver.SetZoneType(ZoneType.Dungeon);
                    SetNameplateViewZone(ZoneType);

                    break;
            }
        }

        var enableHud = _nameplateView.PartyMode is NameplateMode.RoleLetters or NameplateMode.SmallJobIconAndRole;
        _partyListHudUpdater.EnableUpdates(enableHud);

        Service.Log.Verbose($"Setting modes: nameplates party {_nameplateView.PartyMode} others {_nameplateView.OthersMode}, chat {_chatNameUpdater.PartyMode}, update HUD {enableHud}");
        Service.Log.Debug($"Entered ZoneType {ZoneType.ToString()}");

        Service.Framework.RunOnFrameworkThread(NameplateUpdater.ForceRedrawNamePlates);
    }
}