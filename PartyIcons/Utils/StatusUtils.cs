using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using OnlineStatus = Lumina.Excel.GeneratedSheets.OnlineStatus;

namespace PartyIcons.Utils;

public static class StatusUtils
{
    private const Status LastKnownStatus = Status.Online;
    private static readonly Dictionary<Status, uint> IconIdCache = new();
    private static List<OnlineStatus>? _excelStatus;

    public static uint OnlineStatusToIconId(Status status)
    {
        if (IconIdCache.TryGetValue(status, out var cached)) {
            return cached;
        }

        var iconId = ExcelStatus
            .Where(row => row.RowId == (uint)status)
            .Select(row => row.Icon)
            .FirstOrDefault();
        IconIdCache.Add(status, iconId);
        return iconId;
    }

    public static List<OnlineStatus> ExcelStatus => _excelStatus ??= Service.DataManager.GameData.GetExcelSheet<OnlineStatus>()!.ToList();

    public static Status ToStatus(this OnlineStatus onlineStatus)
    {
        return (Status)onlineStatus.RowId;
    }

    private static readonly Status[] KnownStatuses = Enum.GetValues<Status>();

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

    public static StatusVisibility[] ListsToArray(List<Status> important, List<Status> show)
    {
        var array = new StatusVisibility[ExcelStatus.Count];
        Array.Fill(array, StatusVisibility.Unknown);

        foreach (var status in KnownStatuses) {
            if (FixedStatuses.Contains(status)) {
                array[(int)status] = StatusVisibility.Important;
            }
            else if (ConfigurableStatuses.Contains(status)) {
                if (important.Contains(status)) {
                    array[(int)status] = StatusVisibility.Important;
                }
                else if (show.Contains(status)) {
                    array[(int)status] = StatusVisibility.Show;
                }
                else {
                    array[(int)status] = StatusVisibility.Hide;
                }
            }
            else {
                array[(int)status] = StatusVisibility.Unexpected;
            }
        }

        return array;
    }

    public static StatusVisibility[] DictToArray(Dictionary<Status, StatusVisibility> dict)
    {
        var array = new StatusVisibility[ExcelStatus.Count];
        Array.Fill(array, StatusVisibility.Unknown);

        foreach (var status in KnownStatuses) {
            if (FixedStatuses.Contains(status)) {
                array[(int)status] = StatusVisibility.Important;
            }
            else if (ConfigurableStatuses.Contains(status)) {
                array[(int)status] = dict.GetValueOrDefault(status, StatusVisibility.Unset);
            }
            else {
                array[(int)status] = StatusVisibility.Unexpected;
            }
        }

        foreach (var status in FixedStatuses) {
            array[(int)status] = StatusVisibility.Important;
        }

        return array;
    }

    public static Dictionary<Status, StatusVisibility> ArrayToDict(StatusVisibility[] array)
    {
        var dict = new Dictionary<Status, StatusVisibility>();
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