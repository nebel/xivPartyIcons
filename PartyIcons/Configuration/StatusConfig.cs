using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PartyIcons.Entities;
using PartyIcons.Runtime;
using PartyIcons.Utils;

namespace PartyIcons.Configuration;

[Serializable]
public class StatusConfig
{
    public readonly StatusPreset Preset;
    public readonly Guid? Id;
    public string? Name;

    [JsonConverter(typeof(EnumKeyConverter<Status, StatusVisibility>))]
    public Dictionary<Status, StatusVisibility> DisplayMap = new();

    [JsonConstructor]
    private StatusConfig(StatusPreset preset, Guid? id)
    {
        Preset = preset;
        Id = id;
    }

    public StatusConfig(StatusPreset preset)
    {
        Preset = preset;
        Id = null;
        Name = null;
        Reset();
    }

    public StatusConfig(string name)
    {
        Preset = StatusPreset.Custom;
        Id = Guid.NewGuid();
        Name = name;
        Reset();
    }

    public StatusConfig(string name, StatusConfig copyTarget)
    {
        Preset = StatusPreset.Custom;
        Id = Guid.NewGuid();
        Name = name;
        DisplayMap = new Dictionary<Status, StatusVisibility>(copyTarget.DisplayMap);
    }

    public void Reset()
    {
        DisplayMap = StatusUtils.ArrayToDict(Preset switch
        {
            StatusPreset.Custom => Defaults.Custom,
            StatusPreset.Overworld => Defaults.Overworld,
            StatusPreset.Instances => Defaults.Instances,
            StatusPreset.FieldOperations => Defaults.FieldOperations,
            _ => throw new Exception($"Cannot reset status config of unknown type {Preset}")
        });
    }

    private static class Defaults
    {
        public static StatusVisibility[] Overworld => StatusUtils.ListsToArray(
            [],
            [
                Status.EventParticipant,
                Status.Disconnected,
                Status.Busy,
                Status.PlayingTripleTriad,
                Status.ViewingCutscene,
                Status.AwayFromKeyboard,
                Status.CameraMode,
                Status.LookingForRepairs,
                Status.LookingToRepair,
                Status.LookingToMeldMateria,
                Status.Roleplaying,
                Status.LookingForParty,
                Status.WaitingForDutyFinder,
                Status.RecruitingPartyMembers,
                Status.Mentor,
                Status.PvEMentor,
                Status.TradeMentor,
                Status.PvPMentor,
                Status.Returner,
                Status.NewAdventurer,
                Status.AllianceLeader,
                Status.AlliancePartyLeader,
                Status.AlliancePartyMember,
                Status.PartyLeader,
                Status.PartyMember,
                Status.PartyLeaderCrossworld,
                Status.PartyMemberCrossworld,
                Status.InDuty,
                Status.SharingDuty,
                Status.SimilarDuty,
                Status.TrialAdventurer,
            ]);

        public static StatusVisibility[] Instances => StatusUtils.ListsToArray([
                Status.Disconnected,
                Status.ViewingCutscene,
                Status.AwayFromKeyboard,
                Status.CameraMode,
            ],
            [
                Status.Returner,
                Status.NewAdventurer,
            ]);

        public static StatusVisibility[] FieldOperations => StatusUtils.ListsToArray([
                Status.Disconnected,
                Status.ViewingCutscene,
                Status.AwayFromKeyboard,
                Status.CameraMode,
                Status.SharingDuty, // This allows you to see which players don't have a party (note: when?)
            ],
            [
                Status.Returner,
                Status.NewAdventurer,
            ]);

        public static StatusVisibility[] Custom => StatusUtils.ListsToArray([
                Status.Disconnected,
            ],
            []);
    }
}

[Serializable]
public record struct StatusSelector
{
    public StatusPreset Preset;
    public Guid? Id;

    public StatusSelector(StatusPreset preset)
    {
        Preset = preset;
        Id = null;
    }

    public StatusSelector(ZoneType zoneType)
    {
        Preset = zoneType switch
        {
            ZoneType.Overworld => StatusPreset.Overworld,
            ZoneType.Dungeon => StatusPreset.Instances,
            ZoneType.Raid => StatusPreset.Instances,
            ZoneType.AllianceRaid => StatusPreset.Instances,
            ZoneType.FieldOperation => StatusPreset.FieldOperations,
            _ => throw new ArgumentOutOfRangeException(nameof(zoneType), zoneType, null)
        };
        Id = null;
    }

    public StatusSelector(Guid guid)
    {
        Preset = StatusPreset.Custom;
        Id = guid;
    }

    public StatusSelector(StatusConfig config)
    {
        Preset = config.Preset;
        Id = config.Id;
    }

}

public enum StatusPreset
{
    Overworld,
    Instances,
    FieldOperations,

    Custom = 10_000
}

public enum StatusVisibility : byte
{
    Hide = 0,
    Show = 1,
    Important = 2,

    Unset = 253, // Value which is somehow missing from a dict
    Unknown = 254, // Value which is unknown (e.g. new in patch)
    Unexpected = 255 // Value which is known, but we don't think can actually appear as a nameplate status
}