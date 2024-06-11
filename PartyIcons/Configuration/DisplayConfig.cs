using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PartyIcons.Runtime;
using PartyIcons.Utils;

namespace PartyIcons.Configuration;

[Serializable]
public class DisplayConfig
{
    public readonly DisplayPreset Preset;
    public readonly Guid? Id;
    public string? Name;

    public readonly NameplateMode Mode;

    public float Scale;
    public IconSetId IconSetId;
    public StatusSwapStyle SwapStyle;

    [JsonConverter(typeof(EnumKeyConverter<ZoneType, StatusSelector>))]
    public Dictionary<ZoneType, StatusSelector> StatusSelectors = [];

    public IconCustomizeConfig ExIcon;
    public IconCustomizeConfig SubIcon;

    [JsonConstructor]
    private DisplayConfig(DisplayPreset preset, Guid? id, NameplateMode mode)
    {
        Preset = preset;
        Id = id;
        Mode = mode;
    }

    public DisplayConfig(DisplayPreset preset)
    {
        Preset = preset;
        Id = null;
        Name = null;

        Mode = GetBaseModeForPreset(preset);
        Reset();
    }

    public DisplayConfig(string name, NameplateMode mode)
    {
        Preset = DisplayPreset.Custom;
        Id = Guid.NewGuid();
        Name = name;

        Mode = mode;
        Reset();
    }

    public void Reset()
    {
        // Mode = (NameplateMode)Preset;
        Scale = 1f;
        IconSetId = IconSetId.Inherit;
        SwapStyle = Mode is NameplateMode.BigJobIcon or NameplateMode.BigJobIconAndPartySlot
            or NameplateMode.RoleLetters
            ? StatusSwapStyle.Swap
            : StatusSwapStyle.None;
        StatusSelectors = [];
        ExIcon = new IconCustomizeConfig();
        SubIcon = new IconCustomizeConfig();

        if (Mode is NameplateMode.RoleLetters) {
            ExIcon = ExIcon with { Show = false };
        }

        Sanitize();
    }

    public bool Sanitize()
    {
        if (Mode is NameplateMode.Default or NameplateMode.Hide) return false;

        var sanitized = false;
        foreach (var zoneType in Enum.GetValues<ZoneType>()) {
            if (!StatusSelectors.ContainsKey(zoneType)) {
                StatusSelectors[zoneType] = new StatusSelector(zoneType);
                sanitized = true;
            }
        }

        return sanitized;
    }

    private static NameplateMode GetBaseModeForPreset(DisplayPreset preset)
    {
        return preset switch
        {
            DisplayPreset.Default => NameplateMode.Default,
            DisplayPreset.Hide => NameplateMode.Hide,
            DisplayPreset.SmallJobIcon => NameplateMode.SmallJobIcon,
            DisplayPreset.SmallJobIconAndRole => NameplateMode.SmallJobIconAndRole,
            DisplayPreset.BigJobIcon => NameplateMode.BigJobIcon,
            DisplayPreset.BigJobIconAndPartySlot => NameplateMode.BigJobIconAndPartySlot,
            DisplayPreset.RoleLetters => NameplateMode.RoleLetters,
            DisplayPreset.Custom => throw new ArgumentException("DisplayPreset.Custom has no base mode"),
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null)
        };
    }
}

[Serializable]
public record struct DisplaySelector
{
    public DisplayPreset Preset;
    public Guid? Id;

    public DisplaySelector(DisplayPreset preset)
    {
        Preset = preset;
        Id = null;
    }

    public DisplaySelector(NameplateMode mode)
    {
        Preset = mode switch
        {
            NameplateMode.Default => DisplayPreset.Default,
            NameplateMode.Hide => DisplayPreset.Hide,
            NameplateMode.SmallJobIcon => DisplayPreset.SmallJobIcon,
            NameplateMode.SmallJobIconAndRole => DisplayPreset.SmallJobIconAndRole,
            NameplateMode.BigJobIcon => DisplayPreset.BigJobIcon,
            NameplateMode.BigJobIconAndPartySlot => DisplayPreset.BigJobIconAndPartySlot,
            NameplateMode.RoleLetters => DisplayPreset.RoleLetters,
            _ => DisplayPreset.Default
        };
        Id = null;
    }

    public DisplaySelector(Guid guid)
    {
        Preset = DisplayPreset.Custom;
        Id = guid;
    }

    public DisplaySelector(DisplayConfig config)
    {
        Preset = config.Preset;
        Id = config.Id;
    }
}

[Serializable]
public struct IconCustomizeConfig()
{
    public bool Show = true;
    public float Scale = 1f;
    public short OffsetX;
    public short OffsetY;
}

public enum DisplayPreset
{
    Default,
    Hide,
    SmallJobIcon,
    SmallJobIconAndRole,
    BigJobIcon,
    BigJobIconAndPartySlot,
    RoleLetters,

    Custom = 10_000
}

public enum StatusSwapStyle
{
    None,
    Swap,
    Replace
}