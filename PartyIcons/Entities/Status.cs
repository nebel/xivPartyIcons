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
    Online = 45
}

public static class StatusUtils
{
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
    {
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
    };

    public static readonly Status[] PriorityStatusesInDuty =
    {
        Status.Disconnected,
        Status.ViewingCutscene,
        Status.AwayFromKeyboard,
        Status.CameraMode
    };

    public static readonly Status[] PriorityStatusesInForay =
    {
        Status.SharingDuty, // This allows you to see which players don't have a party
        Status.Disconnected,
        Status.ViewingCutscene,
        Status.AwayFromKeyboard,
        Status.CameraMode
    };

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