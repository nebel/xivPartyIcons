using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

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

    [JsonIgnore]
    public IEnumerable<DisplayConfig> Configs => new Enumerable(this);

    [JsonIgnore]
    public IEnumerable<DisplaySelector> Selectors => Configs.Select(c => new DisplaySelector(c));

    private class Enumerable(DisplayConfigs configs) : IEnumerable<DisplayConfig>
    {
        public IEnumerator<DisplayConfig> GetEnumerator()
        {
            yield return configs.Default;
            yield return configs.Hide;
            yield return configs.SmallJobIcon;
            yield return configs.SmallJobIconAndRole;
            yield return configs.BigJobIcon;
            yield return configs.BigJobIconAndPartySlot;
            yield return configs.RoleLetters;
            foreach (var custom in configs.Custom) {
                yield return custom;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public void RemoveSelectors(StatusConfig statusConfig)
    {
        if (statusConfig.Id == null) {
            Service.Log.Warning($"Unexpected null id for config {statusConfig.Preset}/{statusConfig.Name}");
            return;
        }

        List<Action> actions = [];
        foreach (var displayConfig in Configs) {
            foreach (var (zoneType, candidateSelector) in displayConfig.StatusSelectors) {
                if (candidateSelector.Id == statusConfig.Id) {
                    Service.Log.Debug($"Resetting {zoneType}");
                    actions.Add(() => displayConfig.StatusSelectors[zoneType] = new StatusSelector(zoneType));
                }
            }
        }

        foreach (var action in actions) {
            action();
        }
    }
}