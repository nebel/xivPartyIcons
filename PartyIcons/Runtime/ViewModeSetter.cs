using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using Lumina.Excel;
using Lumina.Excel.Sheets;
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
    private ZoneType ZoneType { get; set; } = ZoneType.Overworld;

    private readonly NameplateView _nameplateView;
    private readonly Settings _configuration;
    private readonly ChatNameUpdater _chatNameUpdater;
    private readonly PartyListHUDUpdater _partyListHudUpdater;

    private readonly ExcelSheet<ContentFinderCondition> _contentFinderConditionsSheet;

    public ViewModeSetter(NameplateView nameplateView, Settings configuration, ChatNameUpdater chatNameUpdater,
        PartyListHUDUpdater partyListHudUpdater)
    {
        _nameplateView = nameplateView;
        _configuration = configuration;
        _chatNameUpdater = chatNameUpdater;
        _partyListHudUpdater = partyListHudUpdater;

        _contentFinderConditionsSheet = Service.DataManager.GameData.GetExcelSheet<ContentFinderCondition>() ??
                                        throw new InvalidOperationException();
    }

    private void OnConfigurationSave()
    {
        ForceRefresh();
    }

    public void Enable()
    {
        _configuration.OnSave += OnConfigurationSave;
        Service.ClientState.TerritoryChanged += OnTerritoryChanged;

        _chatNameUpdater.OthersMode = _configuration.ChatOthers;

        ForceRefresh();
    }

    private void ForceRefresh()
    {
        OnTerritoryChanged(0);
    }

    private void Disable()
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
        var maybeContent =
            _contentFinderConditionsSheet.FirstOrNull(t => t.TerritoryType.RowId == Service.ClientState.TerritoryType);

        // The above check is not specific enough in some cases (e.g. Masked Carnivale) so try to find the actual content if possible
        unsafe {
            var gameMain = GameMain.Instance();
            if (gameMain != null) {
                if (GameMain.Instance()->CurrentContentFinderConditionId is var conditionId and not 0) {
                    if (_contentFinderConditionsSheet.GetRowOrDefault(conditionId) is {} conditionContent) {
                        maybeContent = conditionContent;
                    }
                }
            }
        }

        if (maybeContent is not { } content || content.RowId is 0) {
            Service.Log.Verbose($"Content null {Service.ClientState.TerritoryType}");

            ZoneType = ZoneType.Overworld;
            _chatNameUpdater.PartyMode = _configuration.ChatOverworld;
            _nameplateView.SetZoneType(ZoneType);
        }
        else {
            if (_configuration.ChatContentMessage) {
                var sb = new Lumina.Text.SeStringBuilder();
                sb.Append("Entering ");
                sb.Append(content.Name);
                sb.Append(".");
                Service.ChatGui.Print(sb.ToArray(), Service.PluginInterface.InternalName, 45);
            }

            var memberType = content.ContentMemberType.RowId;

            if (content.TerritoryType.ValueNullable is { TerritoryIntendedUse.RowId: 41 or 48 }) {
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
                $"Territory changed {content.Name} (id {content.RowId} type {content.ContentType.RowId}, terr {Service.ClientState.TerritoryType}, iu {content.TerritoryType.ValueNullable?.TerritoryIntendedUse}, memtype {content.ContentMemberType.RowId}, overriden {memberType}, zoneType {ZoneType})");
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
        _partyListHudUpdater.SetRoleVisibility(enableHud);

        Service.Log.Verbose($"Setting modes: nameplates party {_nameplateView.PartyDisplay.Mode} others {_nameplateView.OthersDisplay.Mode}, chat {_chatNameUpdater.PartyMode}, update HUD {enableHud}");
        Service.Log.Debug($"Entered ZoneType {ZoneType.ToString()}");

        Service.Framework.RunOnFrameworkThread(NameplateUpdater.ForceRedrawNamePlates);
    }
}