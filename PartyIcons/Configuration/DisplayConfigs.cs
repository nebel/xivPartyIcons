using System;
using System.Collections.Generic;

namespace PartyIcons.Configuration;

[Serializable]
public class DisplayConfigs
{
    public DisplayConfig Default { get; set; } = new(DisplayPreset.Default);
    public DisplayConfig Hide { get; set; } = new(DisplayPreset.Hide);
    public DisplayConfig SmallJobIcon { get; set; } = new(DisplayPreset.SmallJobIcon);
    public DisplayConfig SmallJobIconAndRole { get; set; } = new(DisplayPreset.SmallJobIconAndRole);
    public DisplayConfig BigJobIcon { get; set; } = new(DisplayPreset.BigJobIcon);
    public DisplayConfig BigJobIconAndPartySlot { get; set; } = new(DisplayPreset.BigJobIconAndPartySlot);
    public DisplayConfig RoleLetters { get; set; } = new(DisplayPreset.RoleLetters);
    public List<DisplayConfig> Custom { get; set; } = [];
}