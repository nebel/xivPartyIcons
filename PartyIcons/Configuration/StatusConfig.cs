using System;
using System.Collections.Generic;
using PartyIcons.Entities;
using PartyIcons.Runtime;
using PartyIcons.Utils;

namespace PartyIcons.Configuration;

[Serializable]
public class StatusConfig
{
    public readonly StatusPreset Preset;
    public readonly Guid Id;
    public string? Name;
    public Dictionary<Status, StatusVisibility> DisplayMap = new();

    public StatusConfig(StatusPreset preset)
    {
        Preset = preset;
        Id = Guid.Empty;
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

    public void Reset()
    {
        if (Preset != StatusPreset.Custom) {
            DisplayMap = StatusUtils.ArrayToDict(Preset switch
            {
                StatusPreset.Custom => Defaults.Custom,
                StatusPreset.Overworld => Defaults.Overworld,
                StatusPreset.Instances => Defaults.Instances,
                StatusPreset.FieldOperations => Defaults.FieldOperations,
                _ => throw new Exception($"Cannot reset status config of unknown type {Preset}")
            });
        }
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
            ],
            [
                Status.SharingDuty, // This allows you to see which players don't have a party (note: when?)
                Status.ViewingCutscene,
                Status.AwayFromKeyboard,
                Status.CameraMode,
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
public struct StatusSelector
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
    Unexpected = 255
}