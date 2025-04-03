using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PartyIcons.Configuration;
using PartyIcons.UI.Utils;
using System;
using System.Linq;

namespace PartyIcons.UI.Settings;

public static class ChatNameTab
{
    public static void Draw()
    {
        ImGuiExt.Spacer(2);

        ImGuiExt.SectionHeader("Overworld");

        using (ImRaii.PushIndent(15f)) {
            ChatModeSection("##chat_overworld",
                () => Plugin.Settings.ChatOverworld,
                (config) => Plugin.Settings.ChatOverworld = config,
                "Party:");

            ChatModeSection("##chat_others",
                () => Plugin.Settings.ChatOthers,
                (config) => Plugin.Settings.ChatOthers = config,
                "Others:");
        }

        ImGuiExt.SectionHeader("Instances");

        using (ImRaii.PushIndent(15f)) {
            ChatModeSection("##chat_dungeon",
                () => Plugin.Settings.ChatDungeon,
                (config) => Plugin.Settings.ChatDungeon = config,
                "Dungeon:");

            ChatModeSection("##chat_raid",
                () => Plugin.Settings.ChatRaid,
                (config) => Plugin.Settings.ChatRaid = config,
                "Raid:");

            ChatModeSection("##chat_alliance",
                () => Plugin.Settings.ChatAllianceRaid,
                (config) => Plugin.Settings.ChatAllianceRaid = config,
                "Alliance:");

            ChatModeSection("##chat_chaotic",
                () => Plugin.Settings.ChatChaoticRaid,
                (config) => Plugin.Settings.ChatChaoticRaid = config,
                "Chaotic:");
        }
    }
    
    private static void ChatModeSection(string label, Func<ChatConfig> getter, Action<ChatConfig> setter, string title = "Chat name: ")
    {
        ChatConfig NewConf = new ChatConfig(ChatMode.GameDefault, true);

        ImGui.Text(title);
        ImGui.SameLine(100f);
        ImGuiExt.SetComboWidth(Enum.GetValues<ChatMode>().Select(UiNames.GetName));

        // hack to fix incorrect configurations
        try
        {
            NewConf = getter();
        }
        catch (ArgumentException ex)
        {
            setter(NewConf);
            Plugin.Settings.Save();
        }

        using (var combo = ImRaii.Combo(label, UiNames.GetName(NewConf.Mode))) {
            if (combo) {
                foreach (var mode in Enum.GetValues<ChatMode>())
                {
                    if (ImGui.Selectable(UiNames.GetName(mode), mode == NewConf.Mode))
                    {
                        NewConf.Mode = mode;;
                        setter(NewConf);
                        Plugin.Settings.Save();
                    }
                }
            }
        }

        ImGui.SameLine();
        var colored = NewConf.UseRoleColor;

        if (ImGui.Checkbox($"Role Color{label}", ref colored))
        {
            NewConf.UseRoleColor = colored;
            setter(NewConf);
            Plugin.Settings.Save();
        }
    }
}