using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using PartyIcons.Configuration;
using PartyIcons.Runtime;
using PartyIcons.UI.Utils;
using System.Linq;
using System.Numerics;

namespace PartyIcons.UI.Settings;

public static class UpgradeGuideTab
{
    public static bool ForceRedisplay { get; set; }

    public static void Draw()
    {
        var buttonSize = new Vector2(100f, 80f) * ImGuiHelpers.GlobalScale;

        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedGreen)) {
            ImGui.TextWrapped("Party Icons Upgrade Guide (for version 1.2)");
        }

        ImGui.TextWrapped("The Dawntrail update of Party Icons brings some changes to how status icons are displayed.");
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey2)) {
            ImGui.TextWrapped("(Status icons are the icons for online statuses such as Disconnected, Viewing Cutscene, Waiting for Duty, Mentor, New Adventurer (sprout), and so on.)");
        }
        ImGui.TextWrapped("From this version, it's now possible to display both job icons and status icons together, and to customize this setting for each nameplate display type.");

        ImGuiExt.Spacer(8);
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudYellow)) {
            ImGui.TextWrapped("How would you like status icons to be displayed?");
        }
        ImGuiExt.Spacer(8);

        if (ImGui.Button("Use the new\n    defaults", buttonSize)) {
            UseNewDefaults();
        }
        ImGui.SameLine();
        ImGui.TextWrapped("In duties, certain important statuses are swapped with the job icon, but otherwise mostly hidden. In the overworld, most statuses are displayed in their own status icon slot.");
        ImGui.Separator();

        if (ImGui.Button("Replicate the\n  old priority\n      system", buttonSize)) {
            ReplicatePriority();
        }
        ImGui.SameLine();
        ImGui.TextWrapped("Certain important status icons (like Disconnected, AFK or Viewing Cutscene) replace job icons entirely when appropriate, but status icons are otherwise hidden.");
        ImGui.Separator();

        if (ImGui.Button(" Don't show\nstatus icons\n      at all", buttonSize)) {
            NoIcons();
        }
        ImGui.SameLine();
        ImGui.TextWrapped("Only job icons will be displayed.");
        ImGui.Separator();

        if (ForceRedisplay) {
            if (ImGui.Button("Cancel", buttonSize)) {
                Cancel();
            }
            ImGui.SameLine();
            ImGui.TextWrapped("No changes will be made.");
            ImGui.Separator();
        }

        ImGui.TextWrapped("Please select one of the above options to continue. You can bring this window up again from the General tab, or customize status icon visibility in much greater detail from the Appearance and Status Icons tabs.");
    }

    private static void UseNewDefaults()
    {
        foreach (var config in Plugin.Settings.DisplayConfigs.Configs.Where(c => c.Preset != DisplayPreset.Custom)) {
            config.StatusIcon.Show = true;
            config.SwapStyle = StatusSwapStyle.Swap;
            config.StatusSelectors[ZoneType.Overworld] = new StatusSelector(StatusPreset.Overworld);
        }
        Plugin.Settings.SelectorsDialogComplete = true;
        Plugin.Settings.Save();
        ForceRedisplay = false;

    }

    private static void ReplicatePriority()
    {
        foreach (var config in Plugin.Settings.DisplayConfigs.Configs.Where(c => c.Preset != DisplayPreset.Custom)) {
            config.StatusIcon.Show = true;
            config.SwapStyle = StatusSwapStyle.Replace;
            config.StatusSelectors[ZoneType.Overworld] = new StatusSelector(StatusPreset.OverworldLegacy);
        }
        Plugin.Settings.SelectorsDialogComplete = true;
        Plugin.Settings.Save();
        ForceRedisplay = false;
    }

    private static void NoIcons()
    {
        foreach (var config in Plugin.Settings.DisplayConfigs.Configs.Where(c => c.Preset != DisplayPreset.Custom)) {
            config.StatusIcon.Show = false;
        }
        Plugin.Settings.SelectorsDialogComplete = true;
        Plugin.Settings.Save();
        ForceRedisplay = false;
    }

    private static void Cancel()
    {
        ForceRedisplay = false;
    }
}