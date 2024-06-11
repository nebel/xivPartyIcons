using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using ImGuiNET;
using PartyIcons.Configuration;
using PartyIcons.Runtime;
using PartyIcons.Utils;
using Action = System.Action;

namespace PartyIcons.UI;

public sealed class AppearanceSettings
{
    private NameplateMode _createMode = NameplateMode.SmallJobIcon;

    public void DrawAppearanceSettings()
    {
        const float separatorPadding = 2f;
        ImGui.Dummy(new Vector2(0, separatorPadding));

        ImGui.TextDisabled("Configure nameplate appearance");
        ImGui.Dummy(new Vector2(0, separatorPadding));

        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.Text("Presets");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 2f));

        List<Action> actions = [];

        DrawDisplayConfig(Plugin.Settings.DisplayConfigs.SmallJobIcon, ref actions);
        DrawDisplayConfig(Plugin.Settings.DisplayConfigs.SmallJobIconAndRole, ref actions);
        DrawDisplayConfig(Plugin.Settings.DisplayConfigs.BigJobIcon, ref actions);
        DrawDisplayConfig(Plugin.Settings.DisplayConfigs.BigJobIconAndPartySlot, ref actions);
        DrawDisplayConfig(Plugin.Settings.DisplayConfigs.RoleLetters, ref actions);

        ImGui.Dummy(new Vector2(0, 15f));
        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.Text("User-created");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 2f));

        var modes = Enum.GetValues<NameplateMode>()
            .Where(v => v is not (NameplateMode.Default or NameplateMode.Hide))
            .ToList();

        SettingsWindow.SetComboWidth(modes.Select(SettingsWindow.GetName));
        if (ImGui.BeginCombo("##newDisplay", SettingsWindow.GetName(_createMode))) {
            foreach (var mode in modes) {
                if (ImGui.Selectable(SettingsWindow.GetName(mode), mode == _createMode)) {
                    Service.Log.Info($"set to {mode}");
                    _createMode = mode;
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (ImGui.Button("Create new")) {
            Plugin.Settings.DisplayConfigs.Custom.Add(
                new DisplayConfig($"Custom {Plugin.Settings.DisplayConfigs.Custom.Count + 1}", _createMode));
            Plugin.Settings.Save();
        }

        ImGui.Dummy(new Vector2(0, 10));

        foreach (var statusConfig in Plugin.Settings.DisplayConfigs.Custom) {
            DrawDisplayConfig(statusConfig, ref actions);
        }

        foreach (var action in actions) {
            action();
        }
    }

    private void DrawDisplayConfig(DisplayConfig config, ref List<Action> actions)
    {
        using var id = ImRaii.PushId($"display@{config.Preset}@{config.Id}");

        if (!ImGui.CollapsingHeader($"{GetName(config)}###statusHeader@{config.Preset}@{config.Id}"))
            return;

        using var indent = ImRaii.PushIndent();

        if (config.Preset == DisplayPreset.Custom) {
            ImGui.TextDisabled("User-created");
            // ImGui.TextDisabled("Based on: ");
            // ImGui.SameLine();
            ImGui.TextUnformatted(SettingsWindow.GetName(config.Mode));

            var name = config.Name ?? "";
            if (ImGui.InputText("Name##rename", ref name, 100, ImGuiInputTextFlags.EnterReturnsTrue)) {
                actions.Add(() =>
                {
                    config.Name = name.Replace("%", "");
                    Plugin.Settings.Save();
                });
            }

            ImGui.Dummy(new Vector2(0, 6));
        }

        ImGui.TextDisabled("Appearance");

        var iconSetIds = Enum.GetValues<IconSetId>().ToList();
        iconSetIds.Remove(IconSetId.Inherit);
        iconSetIds.Insert(0, IconSetId.Inherit);

        using (var combo = ImRaii.Combo("Icon set", SettingsWindow.GetName(config.IconSetId))) {
            if (combo) {
                foreach (var iconSetId in iconSetIds) {
                    if (ImGui.Selectable(SettingsWindow.GetName(iconSetId), iconSetId == config.IconSetId)) {
                        config.IconSetId = iconSetId;
                        Plugin.Settings.Save();
                    }
                }
            }
        }

        var scale = config.Scale;
        if (ImGui.SliderFloat("Scale", ref scale, 0.3f, 3f)) {
            config.Scale = Math.Clamp(scale, 0.1f, 10f);
            Plugin.Settings.Save();
        }

        ImGuiComponents.HelpMarker("Hold Control and click the slider to input an exact value");

        using (var combo = ImRaii.Combo("Swap style", config.SwapStyle.ToString())) {
            if (combo) {
                foreach (var style in Enum.GetValues<StatusSwapStyle>()) {
                    if (ImGui.Selectable(style.ToString(), style == config.SwapStyle)) {
                        config.SwapStyle = style;
                        Plugin.Settings.Save();
                    }
                }
            }
        }

        ImGuiComponents.HelpMarker(
            """
            Determines how to perform icon swaps for statuses set to 'Important':
            - 'None' does nothing
            - 'Swap' will swap the status icon and job icon positions
            - 'Replace' will move the status icon into the job item slot, leaving the status icon empty
            """);

        ImGui.Dummy(new Vector2(0, 6));
        ImGui.TextDisabled("Job Icon");
        using (ImRaii.PushId("jobIcon")) {
            DrawJobIcon(() => config.ExIcon, icon => config.ExIcon = icon);
        }

        ImGui.TextDisabled("Status Icon");
        using (ImRaii.PushId("statusIcon")) {
            DrawJobIcon(() => config.SubIcon, icon => config.SubIcon = icon);
        }

        ImGui.Dummy(new Vector2(0, 6));
        ImGui.TextDisabled("Status icon visibility by location");
        foreach (var zoneType in Enum.GetValues<ZoneType>()) {
            DrawStatusSelector(config, zoneType);
        }

        ImGui.Dummy(new Vector2(0, 6));
        ImGui.TextDisabled("Other actions");
        if (ImGuiExt.ButtonEnabledWhen(ImGui.GetIO().KeyCtrl, "Reset to default")) {
            config.Reset();
            Plugin.Settings.Save();
        }

        ImGuiExt.HoverTooltip("Hold Control to allow reset");

        if (config.Preset == DisplayPreset.Custom) {
            ImGui.SameLine();
            if (ImGuiExt.ButtonEnabledWhen(ImGui.GetIO().KeyCtrl, "Delete")) {
                actions.Add(() =>
                {
                    Plugin.Settings.DisplaySelectors.RemoveSelectors(config);
                    Plugin.Settings.DisplayConfigs.Custom.RemoveAll(c => c.Id == config.Id);
                    Plugin.Settings.Save();
                });
            }

            ImGuiExt.HoverTooltip("Hold Control to allow deletion");
        }

        ImGui.Dummy(new Vector2(0, 3));
    }

    private void DrawJobIcon(Func<IconCustomizeConfig> getter, Action<IconCustomizeConfig> setter)
    {
        var icon = getter();

        var show = icon.Show;
        if (ImGui.Checkbox("Show", ref show)) {
            setter(icon with { Show = show });
            Plugin.Settings.Save();
        }

        var scale = icon.Scale;
        if (ImGui.SliderFloat("Scale", ref scale, 0.3f, 3f)) {
            setter(icon with { Scale = Math.Clamp(scale, 0.1f, 10f) });
            Plugin.Settings.Save();
        }

        ImGuiComponents.HelpMarker("Hold Control and click the slider to input an exact value");

        int[] pos = [icon.OffsetX, icon.OffsetY];
        if (ImGui.SliderInt2("Offset X/Y", ref pos[0], -50, 50)) {
            var x = (short)Math.Clamp(pos[0], -1000, 1000);
            var y = (short)Math.Clamp(pos[1], -1000, 1000);
            setter(icon with { OffsetX = x, OffsetY = y });
            Plugin.Settings.Save();
        }

        ImGuiComponents.HelpMarker("Hold Control and click a slider to input an exact value");
    }

    private void DrawStatusSelector(DisplayConfig config, ZoneType zoneType)
    {
        var currentSelector = config.StatusSelectors[zoneType];
        SettingsWindow.SetComboWidth(Plugin.Settings.StatusConfigs.Selectors.Select(SettingsWindow.GetName));
        using var combo = ImRaii.Combo($"{SettingsWindow.GetName(zoneType)}##zoneSelector@{zoneType}",
            SettingsWindow.GetName(currentSelector));
        if (!combo) return;

        foreach (var selector in Plugin.Settings.StatusConfigs.Selectors) {
            if (selector.Preset == StatusPreset.Custom) {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.HealerGreen);
            }

            if (ImGui.Selectable(SettingsWindow.GetName(selector), currentSelector == selector)) {
                config.StatusSelectors[zoneType] = selector;
                Plugin.Settings.Save();
            }

            if (selector.Preset == StatusPreset.Custom) {
                ImGui.PopStyleColor();
            }
        }
    }

    private static string GetName(DisplayConfig config)
    {
        if (config.Preset == DisplayPreset.Custom) {
            return $"{SettingsWindow.GetName(config.Mode)} ({config.Name})";
        }

        return SettingsWindow.GetName(config.Mode);
    }
}