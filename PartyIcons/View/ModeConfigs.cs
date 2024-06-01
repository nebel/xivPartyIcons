using System;
using PartyIcons.Configuration;
using PartyIcons.Entities;

namespace PartyIcons.View;

public class ModeConfigs
{
    public ModeConfig SmallJobIcon;
    public ModeConfig SmallJobIconAndRole;
    public ModeConfig BigJobIcon;
    public ModeConfig BigJobIconAndPartySlot;
    public ModeConfig RoleLetters;

    public ModeConfig GetForMode(NameplateMode mode)
    {
        return mode switch
        {
            NameplateMode.Default => default,
            NameplateMode.Hide => default,
            NameplateMode.SmallJobIcon => SmallJobIcon,
            NameplateMode.SmallJobIconAndRole => SmallJobIconAndRole,
            NameplateMode.BigJobIcon => BigJobIcon,
            NameplateMode.BigJobIconAndPartySlot => BigJobIconAndPartySlot,
            NameplateMode.RoleLetters => RoleLetters,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}

public struct ModeConfig
{
    public float GlobalScale = 1;
    public IconConfig ExIconConfig = default;
    public IconConfig SubIconConfig = default;
    public StatusImportance[] Importances = [];

    public ModeConfig()
    {
    }
}

public struct IconConfig
{
    public float Scale = 1f;
    public short OffsetX = 0;
    public short OffsetY = 0;

    public IconConfig()
    {
    }
}