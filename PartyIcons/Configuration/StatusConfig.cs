using System;
using System.Collections.Generic;
using PartyIcons.Entities;
using PartyIcons.Utils;

namespace PartyIcons.Configuration;

public class StatusConfig
{
    public readonly StatusConfigBaseType BaseType;
    public string Name;
    public Dictionary<Status, StatusDisplay> DisplayMap;

    public StatusConfig(StatusConfigBaseType baseType)
    {
        BaseType = baseType;
        Name = baseType.ToString();
        Reset();
    }

    public void Reset()
    {
        if (BaseType != StatusConfigBaseType.Custom) {
            DisplayMap = StatusUtils.ArrayToDict(BaseType switch
            {
                StatusConfigBaseType.Custom => throw new Exception("Cannot reset custom status config"),
                StatusConfigBaseType.Overworld => Defaults.Overworld,
                StatusConfigBaseType.Instances => Defaults.Instances,
                StatusConfigBaseType.FieldOperations => Defaults.FieldOperations,
                _ => throw new Exception($"Cannot reset status config of unknown type {BaseType}")
            });
        }
    }

    private static class Defaults
    {
        public static StatusDisplay[] Overworld => StatusUtils.ListsToArray(
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

        public static StatusDisplay[] Instances => StatusUtils.ListsToArray([
                Status.Disconnected,
                Status.ViewingCutscene,
                Status.AwayFromKeyboard,
                Status.CameraMode,
                Status.Returner,
                Status.NewAdventurer,
            ],
            []);

        public static StatusDisplay[] FieldOperations => StatusUtils.ListsToArray([
                Status.SharingDuty, // This allows you to see which players don't have a party (note: maybe?)
                Status.Disconnected,
                Status.ViewingCutscene,
                Status.AwayFromKeyboard,
                Status.CameraMode,
                Status.Returner,
                Status.NewAdventurer,
            ],
            []);
    }
}

public enum StatusConfigBaseType
{
    Custom,
    Overworld,
    Instances,
    FieldOperations
}

public enum StatusDisplay : byte
{
    Hide = 0,
    Show = 1,
    Important = 2,
    Unexpected = 255
}

