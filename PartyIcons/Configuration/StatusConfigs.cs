using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace PartyIcons.Configuration;

[Serializable]
public class StatusConfigs
{
    public StatusConfig Overworld = new(StatusPreset.Overworld);
    public StatusConfig Instances = new(StatusPreset.Instances);
    public StatusConfig FieldOperations = new(StatusPreset.FieldOperations);
    public List<StatusConfig> Custom { get; set; } = [];

    public IEnumerator<StatusConfig> GetEnumerator()
    {
        yield return Overworld;
        yield return Instances;
        yield return FieldOperations;
        foreach (var custom in Custom) {
            yield return custom;
        }
    }

    [JsonIgnore]
    public IEnumerable<StatusConfig> Configs => new Enumerable(this);

    [JsonIgnore]
    public IEnumerable<StatusSelector> Selectors => Configs.Select(c => new StatusSelector(c));

    private class Enumerable(StatusConfigs configs) : IEnumerable<StatusConfig>
    {
        public IEnumerator<StatusConfig> GetEnumerator()
        {
            yield return configs.Overworld;
            yield return configs.Instances;
            yield return configs.FieldOperations;
            foreach (var custom in configs.Custom) {
                yield return custom;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}