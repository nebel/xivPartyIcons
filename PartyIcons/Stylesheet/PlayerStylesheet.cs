using System;
using System.Text;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.Sheets;
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

    private static ushort GetGenericRoleColor(GenericRole role)
    {
        return role switch
        {
            GenericRole.Tank => 37,
            GenericRole.Melee => 524,
            GenericRole.Ranged => 32,
            GenericRole.Healer => 42,
            _ => FallbackColor
        };
    }

    public static ushort GetRoleColor(RoleId roleId)
    {
        return roleId switch
        {
            RoleId.MT or RoleId.OT => GetGenericRoleColor(GenericRole.Tank),
            RoleId.M1 or RoleId.M2 => GetGenericRoleColor(GenericRole.Melee),
            RoleId.R1 or RoleId.R2 => GetGenericRoleColor(GenericRole.Ranged),
            RoleId.H1 or RoleId.H2 => GetGenericRoleColor(GenericRole.Healer),
            _ => FallbackColor
        };
    }

    public static IconGroupId GetGenericRoleIconGroupId(IconSetId iconSetId, GenericRole role)
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
            IconSetId.Inherit => throw new ArgumentException($"{iconSetId} has no defined appearance"),
            _ => throw new ArgumentException($"Unknown icon set id: {iconSetId}")
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

    public SeString GetGenericRolePlate(GenericRole genericRole, bool colored = true)
    {
        return colored
            ? genericRole switch
            {
                GenericRole.Tank => SeStringUtils.Text(BoxedCharacterString("T"), GetGenericRoleColor(genericRole)),
                GenericRole.Melee => SeStringUtils.Text(BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "M"), GetGenericRoleColor(genericRole)),
                GenericRole.Ranged => SeStringUtils.Text(BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "R"), GetGenericRoleColor(genericRole)),
                GenericRole.Healer => SeStringUtils.Text(BoxedCharacterString("H"), GetGenericRoleColor(genericRole)),
                GenericRole.Crafter => SeStringUtils.Text(BoxedCharacterString("C"), GetGenericRoleColor(genericRole)),
                GenericRole.Gatherer => SeStringUtils.Text(BoxedCharacterString("G"), GetGenericRoleColor(genericRole)),
                _ => SeStringUtils.Text(SeIconChar.BoxedQuestionMark.ToIconString(), GetGenericRoleColor(genericRole))
            }
            : genericRole switch
            {
                GenericRole.Tank => SeStringUtils.Text(BoxedCharacterString("T")),
                GenericRole.Melee => SeStringUtils.Text(BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "M")),
                GenericRole.Ranged => SeStringUtils.Text(BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "R")),
                GenericRole.Healer => SeStringUtils.Text(BoxedCharacterString("H")),
                GenericRole.Crafter => SeStringUtils.Text(BoxedCharacterString("C")),
                GenericRole.Gatherer => SeStringUtils.Text(BoxedCharacterString("G")),
                _ => SeIconChar.BoxedQuestionMark.ToIconString()
            };
    }

    public SeString GetRolePlate(RoleId roleId) =>
        SeStringUtils.Text(BoxedCharacterString(GetRoleName(roleId)), GetRoleColor(roleId));

    public SeString GetPartySlotNumber(uint number, GenericRole genericRole) =>
        SeStringUtils.Text(BoxedCharacterString(number.ToString()), GetGenericRoleColor(genericRole));

    public SeString GetPartySlotNumber(uint number, RoleId role) =>
        SeStringUtils.Text(BoxedCharacterString(number.ToString()), GetRoleColor(role));

    public SeString GetRoleChatPrefix(RoleId roleId) => GetRolePlate(roleId);

    public ushort GetRoleChatColor(RoleId roleId) => GetRoleColor(roleId);

    public ushort GetGenericRoleChatColor(ClassJob classJob) =>
        GetGenericRoleColor(((Job)classJob.RowId).GetRole());

    public SeString GetGenericRoleChatPrefix(ClassJob classJob, bool colored) =>
        GetGenericRolePlate(((Job)classJob.RowId).GetRole(), colored);

    public SeString GetJobChatPrefix(ClassJob classJob, bool colored)
    {
        return colored
            ? new SeString(
                new UIGlowPayload(GetGenericRoleChatColor(classJob)),
                new UIForegroundPayload(GetGenericRoleChatColor(classJob)),
                new TextPayload(classJob.Abbreviation.ParseString()),
                UIForegroundPayload.UIForegroundOff,
                UIGlowPayload.UIGlowOff
            )
            : new SeString(new TextPayload(classJob.Abbreviation.ParseString()));
    }

    public static string BoxedCharacterString(string str)
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

    public static char BoxedOutlinedCharacter(char ch)
    {
        return ch switch
        {
            >= '0' and <= '9' => (char)(ch + 0xE0B0),
            >= 'A' and <= 'Z' => (char)(ch + 0xE030),
            >= 'a' and <= 'z' => (char)(ch + 0xE010),
            _ => ch
        };
    }
}