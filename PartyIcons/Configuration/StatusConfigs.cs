using System;
using System.Collections.Generic;

namespace PartyIcons.Configuration;

[Serializable]
public class StatusConfigs
{
    public StatusConfig Overworld = new(StatusPreset.Overworld);
    public StatusConfig Instances = new(StatusPreset.Instances);
    public StatusConfig FieldOperations = new(StatusPreset.FieldOperations);
    public List<StatusConfig> Custom { get; set; } = [];
}