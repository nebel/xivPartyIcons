using System;
using System.Text;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.Utils;
using PartyIcons.View;

namespace PartyIcons.Stylesheet;

public sealed class PlayerStylesheet
{
    private readonly Settings _configuration;
    private const ushort FallbackColor = 2;

    public PlayerStylesheet(Settings configuration)
    {
        _configuration = configuration;
    }

    private ushort GetGenericRoleColor(GenericRole role)
    {
        switch (role) {
            case GenericRole.Tank:
                return 37;

            case GenericRole.Melee:
                return 524;

            case GenericRole.Ranged:
                return 32;

            case GenericRole.Healer:
                return 42;

            default:
                return FallbackColor;
        }
    }

    public ushort GetRoleColor(RoleId roleId)
    {
        switch (roleId) {
            case RoleId.MT:
            case RoleId.OT:
                return GetGenericRoleColor(GenericRole.Tank);

            case RoleId.M1:
            case RoleId.M2:
                return GetGenericRoleColor(GenericRole.Melee);

            case RoleId.R1:
            case RoleId.R2:
                return GetGenericRoleColor(GenericRole.Ranged);

            case RoleId.H1:
            case RoleId.H2:
                return GetGenericRoleColor(GenericRole.Healer);

            default:
                return FallbackColor;
        }
    }

    public IconGroupId GetGenericRoleIconGroupId(IconSetId iconSetId, GenericRole role)
    {
        return iconSetId switch
        {
            IconSetId.EmbossedFramed => IconGroupId.EmbossedFramed,
            IconSetId.EmbossedFramedSmall => IconGroupId.EmbossedFramedSmall,
            IconSetId.Glowing => IconGroupId.Glowing,
            IconSetId.Gradient => role switch
            {
                GenericRole.Tank => IconGroupId.GradientBlue,
                GenericRole.Melee => IconGroupId.GradientRed,
                GenericRole.Ranged => IconGroupId.GradientOrange,
                GenericRole.Healer => IconGroupId.GradientGreen,
                _ => IconGroupId.GradientGrey
            },
            IconSetId.Embossed => IconGroupId.Embossed,
            _ => throw new ArgumentException($"Unknown icon set id: {_configuration.IconSetId}")
        };
    }

    public string GetRoleName(RoleId roleId)
    {
        return roleId switch
        {
            RoleId.MT => "MT",
            RoleId.OT => _configuration.EasternNamingConvention ? "ST" : "OT",
            RoleId.M1 => _configuration.EasternNamingConvention ? "D1" : "M1",
            RoleId.M2 => _configuration.EasternNamingConvention ? "D2" : "M2",
            RoleId.R1 => _configuration.EasternNamingConvention ? "D3" : "R1",
            RoleId.R2 => _configuration.EasternNamingConvention ? "D4" : "R2",
            _ => roleId.ToString()
        };
    }

    public SeString GetGenericRolePlate(GenericRole genericRole)
    {
        return GetGenericRolePlate(genericRole, true);
    }

    public SeString GetGenericRolePlate(GenericRole genericRole, bool colored)
    {
        return colored
            ? genericRole switch
            {
                GenericRole.Tank => SeStringUtils.Text(BoxedCharacterString("T"), GetGenericRoleColor(genericRole)),
                GenericRole.Melee => SeStringUtils.Text(
                    BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "M"),
                    GetGenericRoleColor(genericRole)),
                GenericRole.Ranged => SeStringUtils.Text(
                    BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "R"),
                    GetGenericRoleColor(genericRole)),
                GenericRole.Healer => SeStringUtils.Text(BoxedCharacterString("H"), GetGenericRoleColor(genericRole)),
                GenericRole.Crafter => SeStringUtils.Text(BoxedCharacterString("C"), GetGenericRoleColor(genericRole)),
                GenericRole.Gatherer => SeStringUtils.Text(BoxedCharacterString("G"), GetGenericRoleColor(genericRole)),
                _ => SeStringUtils.Text(SeIconChar.BoxedQuestionMark.ToIconString(), GetGenericRoleColor(genericRole))
            }
            : genericRole switch
            {
                GenericRole.Tank => SeStringUtils.Text(BoxedCharacterString("T")),
                GenericRole.Melee => SeStringUtils.Text(
                    BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "M")),
                GenericRole.Ranged => SeStringUtils.Text(
                    BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "R")),
                GenericRole.Healer => SeStringUtils.Text(BoxedCharacterString("H")),
                GenericRole.Crafter => SeStringUtils.Text(BoxedCharacterString("C")),
                GenericRole.Gatherer => SeStringUtils.Text(BoxedCharacterString("G")),
                _ => SeIconChar.BoxedQuestionMark.ToIconString()
            };
    }

    public SeString GetRolePlate(RoleId roleId)
    {
        switch (roleId) {
            case RoleId.MT:
                return SeStringUtils.Text(BoxedCharacterString("MT"), GetRoleColor(roleId));

            case RoleId.OT:
                return SeStringUtils.Text(BoxedCharacterString(_configuration.EasternNamingConvention ? "ST" : "OT"),
                    GetRoleColor(roleId));

            case RoleId.M1:
            case RoleId.M2:
                return SeStringUtils.Text(
                    BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "M") +
                    GetRolePlateNumber(roleId), GetRoleColor(roleId));

            case RoleId.R1:
            case RoleId.R2:
                return SeStringUtils.Text(
                    BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "R") +
                    GetRolePlateNumber(roleId), GetRoleColor(roleId));

            case RoleId.H1:
            case RoleId.H2:
                return SeStringUtils.Text(BoxedCharacterString("H") + GetRolePlateNumber(roleId),
                    GetRoleColor(roleId));

            default:
                return string.Empty;
        }
    }

    public SeString GetRolePlateNumber(RoleId roleId)
    {
        if (_configuration.EasternNamingConvention) {
            return roleId switch
            {
                RoleId.MT => SeStringUtils.Text(BoxedCharacterString("1"), GetRoleColor(roleId)),
                RoleId.OT => SeStringUtils.Text(BoxedCharacterString("2"), GetRoleColor(roleId)),
                RoleId.H1 => SeStringUtils.Text(BoxedCharacterString("1"), GetRoleColor(roleId)),
                RoleId.H2 => SeStringUtils.Text(BoxedCharacterString("2"), GetRoleColor(roleId)),
                RoleId.M1 => SeStringUtils.Text(BoxedCharacterString("1"), GetRoleColor(roleId)),
                RoleId.M2 => SeStringUtils.Text(BoxedCharacterString("2"), GetRoleColor(roleId)),
                RoleId.R1 => SeStringUtils.Text(BoxedCharacterString("3"), GetRoleColor(roleId)),
                RoleId.R2 => SeStringUtils.Text(BoxedCharacterString("4"), GetRoleColor(roleId))
            };
        }
        else {
            return roleId switch
            {
                RoleId.MT => SeStringUtils.Text(BoxedCharacterString("1"), GetRoleColor(roleId)),
                RoleId.OT => SeStringUtils.Text(BoxedCharacterString("2"), GetRoleColor(roleId)),
                RoleId.H1 => SeStringUtils.Text(BoxedCharacterString("1"), GetRoleColor(roleId)),
                RoleId.H2 => SeStringUtils.Text(BoxedCharacterString("2"), GetRoleColor(roleId)),
                RoleId.M1 => SeStringUtils.Text(BoxedCharacterString("1"), GetRoleColor(roleId)),
                RoleId.M2 => SeStringUtils.Text(BoxedCharacterString("2"), GetRoleColor(roleId)),
                RoleId.R1 => SeStringUtils.Text(BoxedCharacterString("1"), GetRoleColor(roleId)),
                RoleId.R2 => SeStringUtils.Text(BoxedCharacterString("2"), GetRoleColor(roleId))
            };
        }
    }

    public SeString GetPartySlotNumber(uint number, GenericRole genericRole) =>
        SeStringUtils.Text(BoxedCharacterString(number.ToString()), GetGenericRoleColor(genericRole));

    public SeString GetRoleChatPrefix(RoleId roleId) => GetRolePlate(roleId);

    public ushort GetRoleChatColor(RoleId roleId) => GetRoleColor(roleId);

    public SeString GetGenericRoleChatPrefix(ClassJob classJob, bool colored) =>
        GetGenericRolePlate(((Job)classJob.RowId).GetRole(), colored);

    public ushort GetGenericRoleChatColor(ClassJob classJob) =>
        GetGenericRoleColor(((Job)classJob.RowId).GetRole());

    public SeString GetJobChatPrefix(ClassJob classJob, bool colored)
    {
        if (true) {
            return colored
                ? new SeString(
                    new UIGlowPayload(GetGenericRoleChatColor(classJob)),
                    new UIForegroundPayload(GetGenericRoleChatColor(classJob)),
                    new TextPayload(classJob.Abbreviation),
                    UIForegroundPayload.UIForegroundOff,
                    UIGlowPayload.UIGlowOff
                )
                : new SeString(
                    new TextPayload(classJob.Abbreviation)
                );
        }
    }

    public ushort GetJobChatColor(ClassJob classJob) =>
        GetGenericRoleColor(JobExtensions.GetRole((Job)classJob.RowId));

    public string BoxedCharacterString(string str)
    {
        var builder = new StringBuilder(str.Length);

        foreach (var ch in str) {
            builder.Append(BoxedCharacter(ch));
        }

        return builder.ToString();
    }

    public static char BoxedCharacter(char ch)
    {
        return ch switch
        {
            >= '0' and <= '9' => (char)(ch + 0xE05F),
            >= 'A' and <= 'Z' => (char)(ch + 0xE030),
            >= 'a' and <= 'z' => (char)(ch + 0xE010),
            _ => ch
        };
    }
}