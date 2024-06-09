using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Dalamud.Game.Text.Sanitizer;
using PartyIcons.Runtime;

namespace PartyIcons.Configuration;

[Serializable]
public class DisplayConfig
{
    public readonly DisplayPreset Preset;
    public readonly Guid Id;
    public string? Name;

    public NameplateMode Mode;

    public StatusSwapStyle SwapStyle;
    public Dictionary<ZoneType, StatusSelector> StatusSelectors;

    public float Scale;
    public IconSetId? IconSetId;
    public IconCustomizeConfig ExIcon;
    public IconCustomizeConfig SubIcon;

    public DisplayConfig(DisplayPreset preset)
    {
        Preset = preset;
        Id = Guid.Empty;
        Name = null;
        Mode = (NameplateMode)preset;
        SwapStyle = Mode is NameplateMode.BigJobIcon or NameplateMode.BigJobIconAndPartySlot or NameplateMode.RoleLetters ? StatusSwapStyle.Swap : StatusSwapStyle.None;
        StatusSelectors = [];
        Scale = 1f;
        ExIcon = new IconCustomizeConfig();
        SubIcon = new IconCustomizeConfig();

        Sanitize();
    }

    private void Sanitize()
    {
        if (Mode is NameplateMode.Default or NameplateMode.Hide) return;

        foreach (var zoneType in Enum.GetValues<ZoneType>()) {
            if (!StatusSelectors.ContainsKey(zoneType)) {
                StatusSelectors[zoneType] = new StatusSelector(zoneType);
            }
        }
    }
}

[Serializable]
public struct DisplaySelector
{
    public DisplayPreset Preset;
    public Guid? Id;

    public DisplaySelector(DisplayPreset preset)
    {
        Preset = preset;
        Id = null;
    }

    public DisplaySelector(Guid guid)
    {
        Preset = DisplayPreset.Custom;
        Id = guid;
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