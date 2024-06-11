using System;
using System.Linq;

namespace PartyIcons.Configuration;

[Serializable]
public class DisplaySelectors
{
    public DisplaySelector DisplayOverworld { get; set; } = new(DisplayPreset.SmallJobIcon);
    public DisplaySelector DisplayDungeon { get; set; } = new(DisplayPreset.BigJobIconAndPartySlot);
    public DisplaySelector DisplayRaid { get; set; } = new(DisplayPreset.RoleLetters);
    public DisplaySelector DisplayAllianceRaid { get; set; } = new(DisplayPreset.BigJobIconAndPartySlot);
    public DisplaySelector DisplayFieldOperationParty { get; set; } = new(DisplayPreset.BigJobIconAndPartySlot);
    public DisplaySelector DisplayFieldOperationOthers { get; set; } = new(DisplayPreset.SmallJobIcon);
    public DisplaySelector DisplayOthers { get; set; } = new(DisplayPreset.SmallJobIcon);

    public void RemoveSelectors(DisplayConfig config)
    {
        if (config.Id == null) {
            Service.Log.Warning($"Unexpected null id for config {config.Preset}/{config.Mode}/{config.Name}");
            return;
        }

        foreach (var info in GetType().GetProperties().Where(f => typeof(DisplaySelector).IsAssignableFrom(f.PropertyType))) {
            var val = (DisplaySelector)info.GetValue(this)!;
            if (val.Id == config.Id) {
                Service.Log.Debug($"Resetting {info.Name} to {config.Mode}");
                info.SetValue(this, new DisplaySelector(config.Mode));
            }
        }
    }
}