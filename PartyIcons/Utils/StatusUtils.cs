using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using PartyIcons.Configuration;
using PartyIcons.Entities;

namespace PartyIcons.Utils;

public static class StatusUtils
{
    private const int StatusCount = (int)(Status.Online + 1);
    private const int StatusLookupArrayLength = StatusCount + 10; // Adding some buffer for patches

    private static readonly Dictionary<Status, uint> IconIdCache = new();

    public static uint OnlineStatusToIconId(Status status)
    {
        if (IconIdCache.TryGetValue(status, out var cached)) {
            return cached;
        }

        var lookupResult = Service.DataManager.GameData.GetExcelSheet<Lumina.Excel.GeneratedSheets.OnlineStatus>()
            ?.GetRow((uint)status)?.Icon;
        if (lookupResult is not { } iconId) return 0;
        IconIdCache.Add(status, iconId);
        return iconId;
    }

    private static readonly Status[] AllStatuses = Enum.GetValues<Status>();

    private static readonly Status[] FixedStatuses =
    [
        Status.GameQA,
        Status.GameMasterRed,
        Status.GameMasterBlue,
    ];

    public static readonly Status[] ConfigurableStatuses =
    [
        // Status.GameQA, // Always shown
        // Status.GameMasterRed, // Always shown
        // Status.GameMasterBlue, // Always shown
        Status.EventParticipant,
        Status.Disconnected,
        // Status.WaitingForFriendListApproval, // Not displayed in nameplates
        // Status.WaitingForLinkshellApproval, // Not displayed in nameplates
        // Status.WaitingForFreeCompanyApproval, // Not displayed in nameplates
        // Status.NotFound, // Not displayed in nameplates
        // Status.Offline, // Not displayed in nameplates, Disconnected is used instead
        // Status.BattleMentor, // Not used in game, PvEMentor is used instead
        Status.Busy,
        // Status.PvP, // Not displayed in nameplates
        Status.PlayingTripleTriad,
        Status.ViewingCutscene,
        // Status.UsingChocoboPorter, // Not displayed in nameplates
        Status.AwayFromKeyboard,
        Status.CameraMode,
        Status.LookingForRepairs,
        Status.LookingToRepair,
        Status.LookingToMeldMateria,
        Status.Roleplaying,
        Status.LookingForParty,
        // Status.SwordForHire, // Not used in game
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
        // Status.AnotherWorld, // Not displayed in nameplates
        Status.SharingDuty, // When is this displayed?
        Status.SimilarDuty, // When is this displayed?
        Status.InDuty,
        Status.TrialAdventurer,
        // Status.FreeCompany, // Not displayed in nameplates
        // Status.GrandCompany, // Not displayed in nameplates
        // Status.Online, // Not displayed in nameplates
    ];

    public static StatusDisplay[] ListsToArray(List<Status> important, List<Status> show)
    {
        var array = new StatusDisplay[StatusLookupArrayLength];

        foreach (var status in AllStatuses) {
            if (FixedStatuses.Contains(status)) {
                array[(int)status] = StatusDisplay.Important;
            }
            else if (ConfigurableStatuses.Contains(status)) {
                if (important.Contains(status)) {
                    array[(int)status] = StatusDisplay.Important;
                }
                else if (show.Contains(status)) {
                    array[(int)status] = StatusDisplay.Show;
                }
            }
            else {
                array[(int)status] = StatusDisplay.Unexpected;
            }
        }

        return array;
    }

    public static StatusDisplay[] DictToArray(Dictionary<Status, StatusDisplay> dict)
    {
        var array = new StatusDisplay[StatusLookupArrayLength];

        foreach (var status in AllStatuses) {
            if (FixedStatuses.Contains(status)) {
                array[(int)status] = StatusDisplay.Important;
            }
            else if (ConfigurableStatuses.Contains(status)) {
                if (dict.TryGetValue(status, out var importance)) {
                    array[(int)status] = importance;
                }
            }
            else {
                array[(int)status] = StatusDisplay.Unexpected;
            }
        }

        foreach (var status in FixedStatuses) {
            array[(int)status] = StatusDisplay.Important;
        }

        return array;
    }

    public static Dictionary<Status, StatusDisplay> ArrayToDict(StatusDisplay[] array)
    {
        var dict = new Dictionary<Status, StatusDisplay>();
        for (var i = 0; i < array.Length; i++) {
            dict[(Status)i] = array[i];
        }

        return dict;
    }

    public static BitmapFontIcon OnlineStatusToBitmapIcon(Status status)
    {
        return status switch
        {
            Status.EventParticipant => BitmapFontIcon.Meteor,
            Status.Roleplaying => BitmapFontIcon.RolePlaying,
            Status.Disconnected => BitmapFontIcon.Disconnecting,
            Status.Busy => BitmapFontIcon.DoNotDisturb,
            Status.NewAdventurer => BitmapFontIcon.NewAdventurer,
            Status.Returner => BitmapFontIcon.Returner,
            Status.Mentor => BitmapFontIcon.Mentor,
            Status.BattleMentor => BitmapFontIcon.MentorPvE,
            Status.PvEMentor => BitmapFontIcon.MentorPvE,
            Status.TradeMentor => BitmapFontIcon.MentorCrafting,
            Status.PvPMentor => BitmapFontIcon.MentorPvP,
            Status.WaitingForDutyFinder => BitmapFontIcon.WaitingForDutyFinder,
            _ => BitmapFontIcon.None
        };
    }
}