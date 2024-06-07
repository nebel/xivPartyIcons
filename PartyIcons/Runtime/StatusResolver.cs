using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.Utils;

namespace PartyIcons.Runtime;

public class StatusResolver
{
    private readonly Settings _settings;
    private ZoneType _currentZoneType;
    private StatusDisplay[] _display = null!;

    public StatusResolver(Settings settings)
    {
        _settings = settings;
        _currentZoneType = ZoneType.FieldOperation;
        SetZoneType(ZoneType.Overworld);
    }

    public void SetZoneType(ZoneType zoneType)
    {
        if (zoneType == _currentZoneType) {
            return;
        }

        _currentZoneType = zoneType;

        switch (zoneType) {
            case ZoneType.Overworld:
                LoadConfig(_settings.StatusConfigOverworld);
                break;
            case ZoneType.Dungeon:
            case ZoneType.Raid:
            case ZoneType.AllianceRaid:
                LoadConfig(_settings.StatusConfigInstances);
                break;
            case ZoneType.FieldOperation:
                LoadConfig(_settings.StatusConfigFieldOperations);
                break;
            default:
                LoadConfig(_settings.StatusConfigOverworld);
                break;
        }
    }

    public StatusDisplay CheckStatusDisplay(Status status)
    {
        // Service.Log.Info($"z {_currentZoneType}");
        return _display[(int)status];
    }

    private void LoadConfig(StatusConfig config)
    {
        _display = StatusUtils.DictToArray(config.DisplayMap);
    }
}