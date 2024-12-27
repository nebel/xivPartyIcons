using System;
using PartyIcons.Configuration;
using PartyIcons.Runtime;

namespace PartyIcons.UI.Utils;

public static class UiNames
{
    public static string GetName(NameplateMode mode)
    {
        return mode switch
        {
            NameplateMode.Default => "Game default",
            NameplateMode.Hide => "Hide",
            NameplateMode.BigJobIcon => "Big job icon",
            NameplateMode.SmallJobIcon => "Small job icon and name",
            NameplateMode.SmallJobIconAndRole => "Small job icon, role and name",
            NameplateMode.BigJobIconAndPartySlot => "Big job icon and party number",
            NameplateMode.RoleLetters => "Role letters",
            _ => $"Unknown ({(int)mode}/{mode.ToString()})"
        };
    }

    public static string GetName(ZoneType zoneType)
    {
        return zoneType switch
        {
            ZoneType.Overworld => "Overworld",
            ZoneType.Dungeon => "Dungeon",
            ZoneType.Raid => "Raid",
            ZoneType.AllianceRaid => "Alliance Raid",
            ZoneType.ChaoticRaid => "Chaotic Raid",
            ZoneType.FieldOperation => "Field Operation",
            _ => $"Unknown ({(int)zoneType}/{zoneType.ToString()})"
        };
    }

    public static string GetName(StatusConfig config)
    {
        return config.Preset switch
        {
            StatusPreset.Custom => config.Name ?? "<unnamed>",
            StatusPreset.Overworld => "Overworld",
            StatusPreset.Instances => "Instances",
            StatusPreset.FieldOperations => "Field Operations",
            StatusPreset.OverworldLegacy => "Overworld (Legacy)",
            _ => config.Preset + "/" + config.Name + "/" + config.Id
        };
    }

    public static string GetName(StatusSelector selector)
    {
        return GetName(Plugin.Settings.GetStatusConfig(selector));
    }

    public static string GetName(DisplayConfig config)
    {
        if (config.Preset == DisplayPreset.Custom) {
            return $"{GetName(config.Mode)} ({config.Name})";
        }

        return GetName(config.Mode);
    }

    public static string GetName(DisplaySelector selector)
    {
        return GetName(Plugin.Settings.GetDisplayConfig(selector));
    }

    public static string GetName(IconSetId id)
    {
        return id switch
        {
            IconSetId.EmbossedFramed => "Framed, role colored",
            IconSetId.EmbossedFramedSmall => "Framed, role colored (small)",
            IconSetId.Gradient => "Gradient, role colored",
            IconSetId.Glowing => "Glowing",
            IconSetId.Embossed => "Embossed",
            IconSetId.Inherit => "<Use global setting>",
            _ => id.ToString()
        };
    }

    public static string GetName(ChatMode mode)
    {
        return mode switch
        {
            ChatMode.GameDefault => "Game Default",
            ChatMode.Role => "Role",
            ChatMode.Job => "Job abbreviation",
            _ => throw new ArgumentException($"Unknown chat mode {mode}")
        };
    }

    public static string GetName(RoleDisplayStyle style)
    {
        return style switch
        {
            RoleDisplayStyle.None => "None",
            RoleDisplayStyle.Role => "Role",
            RoleDisplayStyle.PartyNumber => "Party Number",
            _ => throw new ArgumentException($"Unknown RoleDisplayStyle {style}")
        };
    }
}