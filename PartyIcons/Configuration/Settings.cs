using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PartyIcons.Entities;

namespace PartyIcons.Configuration;

[Serializable]
public class Settings : IPluginConfiguration
{
    public static int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;
    public bool ImportedSelectors = false;

    public bool ChatContentMessage = true;
    public bool HideLocalPlayerNameplate = false;
    public bool TestingMode = false;
    public bool EasternNamingConvention = false;
    public bool DisplayRoleInPartyList = false;
    public bool UseContextMenu = false;
    public bool UseContextMenuSubmenu = true;
    public bool AssignFromChat = true;
    public bool UsePriorityIcons = true;

    public IconSetId IconSetId { get; set; } = IconSetId.Gradient;
    public NameplateSizeMode SizeMode { get; set; } = NameplateSizeMode.Medium;
    public float SizeModeCustom { get; set; } = 1f;

    public NameplateMode NameplateOverworld { get; set; } = NameplateMode.SmallJobIcon;
    public NameplateMode NameplateAllianceRaid { get; set; } = NameplateMode.BigJobIconAndPartySlot;
    public NameplateMode NameplateDungeon { get; set; } = NameplateMode.BigJobIconAndPartySlot;
    public NameplateMode NameplateBozjaParty { get; set; } = NameplateMode.BigJobIconAndPartySlot;
    public NameplateMode NameplateBozjaOthers { get; set; } = NameplateMode.Default;
    public NameplateMode NameplateRaid { get; set; } = NameplateMode.RoleLetters;
    public NameplateMode NameplateOthers { get; set; } = NameplateMode.SmallJobIcon;
    public DisplaySelectors DisplaySelectors { get; set; } = new();
    public DisplayConfigs DisplayConfigs { get; set; } = new();
    public StatusConfigs StatusConfigs { get; set; } = new();
    public ChatConfig ChatOverworld { get; set; } = new(ChatMode.Role);
    public ChatConfig ChatAllianceRaid { get; set; } = new(ChatMode.Role);
    public ChatConfig ChatDungeon { get; set; } = new(ChatMode.Job);
    public ChatConfig ChatRaid { get; set; } = new(ChatMode.Role);
    public ChatConfig ChatOthers { get; set; } = new(ChatMode.Job);

    public Dictionary<string, RoleId> StaticAssignments { get; set; } = new();

    public event Action OnSave;

    public Settings()
    {
    }

    public Settings(SettingsV1 configV1)
    {
        ChatContentMessage = configV1.ChatContentMessage;
        HideLocalPlayerNameplate = configV1.HideLocalPlayerNameplate;
        TestingMode = configV1.TestingMode;
        EasternNamingConvention = configV1.EasternNamingConvention;
        DisplayRoleInPartyList = configV1.DisplayRoleInPartyList;
        UseContextMenu = configV1.UseContextMenu;
        AssignFromChat = configV1.AssignFromChat;
        UsePriorityIcons = configV1.UsePriorityIcons;

        IconSetId = configV1.IconSetId;
        SizeMode = configV1.SizeMode;
        NameplateOverworld = configV1.NameplateOverworld;
        NameplateAllianceRaid = configV1.NameplateAllianceRaid;
        NameplateDungeon = configV1.NameplateDungeon;
        NameplateBozjaParty = configV1.NameplateBozjaParty;
        NameplateBozjaOthers = configV1.NameplateBozjaOthers;
        NameplateRaid = configV1.NameplateRaid;
        NameplateOthers = configV1.NameplateOthers;

        ChatOverworld = SettingsV1.ToChatConfig(configV1.ChatOverworld);
        ChatAllianceRaid = SettingsV1.ToChatConfig(configV1.ChatAllianceRaid);
        ChatDungeon = SettingsV1.ToChatConfig(configV1.ChatDungeon);
        ChatRaid = SettingsV1.ToChatConfig(configV1.ChatRaid);
        ChatOthers = SettingsV1.ToChatConfig(configV1.ChatOthers);

        StaticAssignments = configV1.StaticAssignments;
    }

    public static Settings Load()
    {
        Settings? config = null;

        try {
            var configFileInfo = Service.PluginInterface.ConfigFile;

            if (configFileInfo.Exists) {
                var reader = new StreamReader(configFileInfo.FullName);
                var fileText = reader.ReadToEnd();
                reader.Dispose();

                var versionNumber = GetConfigFileVersion(fileText);

                if (versionNumber == CurrentVersion) {
                    config = JsonConvert.DeserializeObject<Settings>(fileText);
                    Service.Log.Information($"Loaded configuration v{versionNumber} (current)");
                }
                else if (versionNumber == 1) {
                    var configV1 = JsonConvert.DeserializeObject<SettingsV1>(fileText);
                    config = new Settings(configV1);
                    config.Save();
                    Service.Log.Information($"Converted configuration v{versionNumber} to v{CurrentVersion}");
                }
                else {
                    Service.Log.Error($"No reader available for configuration v{versionNumber}");
                }
            }
        }
        catch (Exception e) {
            Service.Log.Error(e, "Could not read configuration.");
        }

        if (config == null) {
            Service.Log.Information("Creating a new configuration.");
            config = new Settings
            {
                ImportedSelectors = true
            };
        }
        else {
            config.Sanitize();
        }

        if (!config.ImportedSelectors) {
            config.DisplaySelectors.DisplayOverworld = new DisplaySelector(config.NameplateOverworld);
            config.DisplaySelectors.DisplayDungeon = new DisplaySelector(config.NameplateDungeon);
            config.DisplaySelectors.DisplayRaid = new DisplaySelector(config.NameplateRaid);
            config.DisplaySelectors.DisplayAllianceRaid = new DisplaySelector(config.NameplateAllianceRaid);
            config.DisplaySelectors.DisplayFieldOperationParty = new DisplaySelector(config.NameplateBozjaParty);
            config.DisplaySelectors.DisplayFieldOperationOthers = new DisplaySelector(config.NameplateBozjaOthers);
            config.DisplaySelectors.DisplayOthers = new DisplaySelector(config.NameplateOthers);
            config.ImportedSelectors = true;
            config.Save();
        }

        return config;
    }

    private void Sanitize()
    {
        var sanitized = false;

        foreach (var displayConfig in DisplayConfigs.Configs) {
            sanitized |= displayConfig.Sanitize();
        }

        if (sanitized) {
            Service.Log.Information($"Re-saving sanitized config");
            Save();
        }
    }

    public void Save()
    {
        Service.PluginInterface.SavePluginConfig(this);
        OnSave?.Invoke();
    }

    private static int GetConfigFileVersion(string fileText)
    {
        var json = JObject.Parse(fileText);

        return json.GetValue("Version")?.Value<int>() ?? 0;
    }

    public StatusConfig GetStatusConfig(StatusSelector selector)
    {
        var configs = StatusConfigs;
        switch (selector.Preset) {
            case StatusPreset.Overworld:
                return configs.Overworld;
            case StatusPreset.Instances:
                return configs.Instances;
            case StatusPreset.FieldOperations:
                return configs.FieldOperations;
            case StatusPreset.Custom:
                foreach (var config in configs.Custom.Where(config => config.Id == selector.Id)) {
                    return config;
                }

                Service.Log.Warning($"Couldn't find custom preset with id {selector.Id}, falling back to overworld");
                return configs.Overworld;
            default:
                Service.Log.Warning($"Couldn't find preset of type {selector.Preset}, falling back to overworld");
                return configs.Overworld;
        }
    }

    public DisplayConfig GetDisplayConfig(DisplaySelector selector)
    {
        var configs = DisplayConfigs;
        switch (selector.Preset) {
            case DisplayPreset.Default:
                return configs.Default;
            case DisplayPreset.Hide:
                return configs.Hide;
            case DisplayPreset.SmallJobIcon:
                return configs.SmallJobIcon;
            case DisplayPreset.SmallJobIconAndRole:
                return configs.SmallJobIconAndRole;
            case DisplayPreset.BigJobIcon:
                return configs.BigJobIcon;
            case DisplayPreset.BigJobIconAndPartySlot:
                return configs.BigJobIconAndPartySlot;
            case DisplayPreset.RoleLetters:
                return configs.RoleLetters;
            case DisplayPreset.Custom:
                foreach (var config in configs.Custom.Where(config => config.Id == selector.Id)) {
                    return config;
                }

                Service.Log.Warning($"Couldn't find custom preset with id {selector.Id}, falling back to default");
                return configs.Default;
            default:
                Service.Log.Warning($"Couldn't find preset of type {selector.Preset}, falling back to default");
                return configs.Default;
        }
    }
}