using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace PartyIcons.Api;

public static unsafe class XivApi
{
    public static void Initialize()
    {
        Service.ClientState.Logout += ResetRaptureAtkModule;
    }

    public static void Dispose()
    {
        Service.ClientState.Logout -= ResetRaptureAtkModule;
    }

    private static RaptureAtkModule* _raptureAtkModulePtr;

    public static RaptureAtkModule* RaptureAtkModulePtr
    {
        get
        {
            if (_raptureAtkModulePtr == null) {
                _raptureAtkModulePtr = Framework.Instance()->GetUiModule()->GetRaptureAtkModule();
            }

            return _raptureAtkModulePtr;
        }
    }

    private static void ResetRaptureAtkModule() => _raptureAtkModulePtr = null;

    public static string PrintRawStringArg(IntPtr arg)
    {
        var seString = MemoryHelper.ReadSeStringNullTerminated(arg);
        return string.Join("", seString.Payloads.Select(payload => $"[{payload}]"));
    }
}

public static class IconConverter
{
    public static BitmapFontIcon StatusIconToBitmapIcon(uint iconId)
    {
        // Low-res versions are +50
        if (iconId > 61550) {
            iconId -= 50;
        }

        return iconId switch
        {
            61533 => BitmapFontIcon.Meteor,
            61545 => BitmapFontIcon.RolePlaying,
            61503 => BitmapFontIcon.Disconnecting,
            61509 => BitmapFontIcon.DoNotDisturb,
            61523 => BitmapFontIcon.NewAdventurer,
            61547 => BitmapFontIcon.Returner,
            61540 => BitmapFontIcon.Mentor,
            61542 => BitmapFontIcon.MentorPvE,
            61543 => BitmapFontIcon.MentorCrafting,
            61544 => BitmapFontIcon.MentorPvP,
            61517 => BitmapFontIcon.WaitingForDutyFinder,
            _ => BitmapFontIcon.None
        };
    }

    public static BitmapFontIcon OnlineStatusToBitmapIcon(OnlineStatus status)
    {
        return status switch
        {
            OnlineStatus.EventParticipant => BitmapFontIcon.Meteor,
            OnlineStatus.Roleplaying => BitmapFontIcon.RolePlaying,
            OnlineStatus.Disconnected => BitmapFontIcon.Disconnecting,
            OnlineStatus.Busy => BitmapFontIcon.DoNotDisturb,
            OnlineStatus.NewAdventurer => BitmapFontIcon.NewAdventurer,
            OnlineStatus.Returner => BitmapFontIcon.Returner,
            OnlineStatus.Mentor => BitmapFontIcon.Mentor,
            OnlineStatus.BattleMentor => BitmapFontIcon.MentorPvE,
            OnlineStatus.PvEMentor => BitmapFontIcon.MentorPvE,
            OnlineStatus.TradeMentor => BitmapFontIcon.MentorCrafting,
            OnlineStatus.PvPMentor => BitmapFontIcon.MentorPvP,
            OnlineStatus.WaitingForDutyFinder => BitmapFontIcon.WaitingForDutyFinder,
            _ => BitmapFontIcon.None
        };
    }

    public static readonly OnlineStatus[] PriorityStatusesInOverworld =
    {
        OnlineStatus.Disconnected,
        OnlineStatus.SharingDuty,
        OnlineStatus.ViewingCutscene,
        OnlineStatus.Busy,
        OnlineStatus.AwayFromKeyboard,
        OnlineStatus.LookingToMeldMateria,
        OnlineStatus.LookingForParty,
        // OnlineStatus.WaitingForDutyFinder,
        OnlineStatus.PartyLeader,
        OnlineStatus.PartyMember,
        OnlineStatus.GameMasterRed,
        OnlineStatus.GameMasterBlue,
        OnlineStatus.EventParticipant,
        OnlineStatus.Roleplaying,
        OnlineStatus.CameraMode
    };

    public static readonly OnlineStatus[] PriorityStatusesInDuty =
    {
        OnlineStatus.Disconnected,
        OnlineStatus.ViewingCutscene,
        OnlineStatus.AwayFromKeyboard,
        OnlineStatus.CameraMode
    };

    public static readonly OnlineStatus[] PriorityStatusesInForay =
    {
        OnlineStatus.SharingDuty, // This allows you to see which players don't have a party
        OnlineStatus.Disconnected,
        OnlineStatus.ViewingCutscene,
        OnlineStatus.AwayFromKeyboard,
        OnlineStatus.CameraMode
    };

    public static readonly OnlineStatus[] NoviceStatuses =
    {
        OnlineStatus.NewAdventurer,
        OnlineStatus.Returner,
        OnlineStatus.Mentor,
        OnlineStatus.BattleMentor,
        OnlineStatus.PvEMentor,
        OnlineStatus.TradeMentor,
        OnlineStatus.PvPMentor
    };

    private static readonly Dictionary<OnlineStatus, uint> IconIdCache = new();

    public static uint OnlineStatusToIconId(OnlineStatus status)
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

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum OnlineStatus : uint
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