using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;

namespace PartyIcons.Entities;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum Status : uint
{
    None = 0,
    GameQA = 1,
    GameMasterRed = 2,
    GameMasterBlue = 3,
    EventParticipant = 4,
    Disconnected = 5,
    WaitingForFriendListApproval = 6,
    WaitingForLinkshellApproval = 7,
    WaitingForFreeCompanyApproval = 8,
    NotFound = 9,
    Offline = 10,
    BattleMentor = 11,
    Busy = 12,
    PvP = 13,
    PlayingTripleTriad = 14,
    ViewingCutscene = 15,
    UsingChocoboPorter = 16,
    AwayFromKeyboard = 17,
    CameraMode = 18,
    LookingForRepairs = 19,
    LookingToRepair = 20,
    LookingToMeldMateria = 21,
    Roleplaying = 22,
    LookingForParty = 23,
    SwordForHire = 24,
    WaitingForDutyFinder = 25,
    RecruitingPartyMembers = 26,
    Mentor = 27,
    PvEMentor = 28,
    TradeMentor = 29,
    PvPMentor = 30,
    Returner = 31,
    NewAdventurer = 32,
    AllianceLeader = 33,
    AlliancePartyLeader = 34,
    AlliancePartyMember = 35,
    PartyLeader = 36,
    PartyMember = 37,
    PartyLeaderCrossworld = 38,
    PartyMemberCrossworld = 39,
    AnotherWorld = 40,
    SharingDuty = 41,
    SimilarDuty = 42,
    InDuty = 43,
    TrialAdventurer = 44,
    FreeCompany = 45,
    GrandCompany = 46,
    Online = 47
}

public enum StatusImportanceCategory : byte
{
    Overworld = 0,
    Duty = 1,
    Foray = 2,
}

public enum StatusImportance : byte
{
    Hide = 0,
    Show = 1,
    Important = 2,
    Alert = 3
}

public static class StatusUtils
{
    public static class Defaults
    {
        public static StatusImportance[] ImportantCombatStatuses = ListsToArray(
        [
            Status.Disconnected,
            Status.ViewingCutscene,
            Status.AwayFromKeyboard,
            Status.CameraMode
        ],
        []);
    }

    public const int StatusCount = (int)(Status.Online + 1);

    public static StatusImportance[] DictToArray(Dictionary<Status, StatusImportance> dict)
    {
        var array = new StatusImportance[StatusCount];

        foreach (var status in AllStatuses) {
            if (FixedStatuses.Contains(status)) {
                array[(int)status] = StatusImportance.Important;
            }
            else if (SupportedStatuses.Contains(status)) {
                if (dict.TryGetValue(status, out var importance)) {
                    array[(int)status] = importance;
                }
            }
            else {
                array[(int)status] = StatusImportance.Alert;
            }
        }

        foreach (var status in FixedStatuses) {
            array[(int)status] = StatusImportance.Important;
        }

        return array;
    }

    public static StatusImportance[] ListsToArray(List<Status> important, List<Status> show)
    {
        var array = new StatusImportance[StatusCount];

        foreach (var status in AllStatuses) {
            if (FixedStatuses.Contains(status)) {
                array[(int)status] = StatusImportance.Important;
            }
            else if (SupportedStatuses.Contains(status)) {
                if (important.Contains(status)) {
                    array[(int)status] = StatusImportance.Important;
                }
                else if (show.Contains(status)) {
                    array[(int)status] = StatusImportance.Show;
                }
            }
            else {
                array[(int)status] = StatusImportance.Alert;
            }
        }

        return array;
    }

    public static readonly Status[] AllStatuses = Enum.GetValues<Status>();

    public static readonly Status[] FixedStatuses =
    [
        Status.GameQA,
        Status.GameMasterRed,
        Status.GameMasterBlue,
    ];

    public static readonly Status[] SupportedStatuses =
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
        // Status.SharingDuty, // Not displayed in nameplates(?)
        // Status.SimilarDuty, // Not displayed in nameplates(?)
        Status.InDuty,
        Status.TrialAdventurer,
        // Status.FreeCompany, // Not displayed in nameplates
        // Status.GrandCompany, // Not displayed in nameplates
        // Status.Online, // Not displayed in nameplates
    ];

    private static readonly Status[] BitmapIconAllowedStatuses =
    [
        // OnlineStatus.EventParticipant,
        Status.Roleplaying,
        // OnlineStatus.Disconnected,
        // OnlineStatus.Busy,
        Status.NewAdventurer,
        Status.Returner,
        Status.Mentor,
        Status.BattleMentor,
        Status.PvEMentor,
        Status.TradeMentor,
        Status.PvPMentor,
        Status.WaitingForDutyFinder
    ];

    public static BitmapFontIcon OnlineStatusToBitmapIcon(Status status)
    {
        if (!BitmapIconAllowedStatuses.Contains(status)) {
            return BitmapFontIcon.None;
        }

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

    public static readonly Status[] PriorityStatusesInOverworld =
    [
        Status.Disconnected,
        Status.SharingDuty,
        Status.ViewingCutscene,
        Status.Busy,
        Status.AwayFromKeyboard,
        Status.LookingToMeldMateria,
        Status.LookingForParty,
        Status.WaitingForDutyFinder,
        Status.PartyLeader,
        Status.PartyMember,
        Status.GameMasterRed,
        Status.GameMasterBlue,
        Status.EventParticipant,
        Status.Roleplaying,
        Status.CameraMode
    ];

    public static readonly Status[] PriorityStatusesInDuty =
    [
        Status.Disconnected,
        Status.ViewingCutscene,
        Status.AwayFromKeyboard,
        Status.CameraMode
    ];

    public static readonly Status[] PriorityStatusesInForay =
    [
        Status.SharingDuty, // This allows you to see which players don't have a party
        Status.Disconnected,
        Status.ViewingCutscene,
        Status.AwayFromKeyboard,
        Status.CameraMode
    ];

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
}