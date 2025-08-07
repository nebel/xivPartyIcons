using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using PartyIcons.Configuration;
using PartyIcons.Runtime;
using PartyIcons.UI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Action = System.Action;

namespace PartyIcons.UI.Settings;

public sealed class AppearanceTab
{
    private NameplateMode _createMode = NameplateMode.SmallJobIcon;

    public void Draw()
    {
        ImGuiExt.Spacer(2);

        ImGui.TextDisabled("Configure nameplate appearance");

        ImGuiExt.SectionHeader("Presets");

        List<Action> actions = [];

        DrawDisplayConfig(Plugin.Settings.DisplayConfigs.SmallJobIcon, ref actions);
        DrawDisplayConfig(Plugin.Settings.DisplayConfigs.SmallJobIconAndRole, ref actions);
        DrawDisplayConfig(Plugin.Settings.DisplayConfigs.BigJobIcon, ref actions);
        DrawDisplayConfig(Plugin.Settings.DisplayConfigs.BigJobIconAndPartySlot, ref actions);
        DrawDisplayConfig(Plugin.Settings.DisplayConfigs.RoleLetters, ref actions);

        ImGuiExt.SectionHeader("User-created");

        var modes = Enum.GetValues<NameplateMode>()
            .Where(v => v is not (NameplateMode.Default or NameplateMode.Hide))
            .ToList();

        ImGuiExt.SetComboWidth(modes.Select(UiNames.GetName));

        using (var combo = ImRaii.Combo("##newDisplay", UiNames.GetName(_createMode))) {
            if (combo) {
                foreach (var mode in modes) {
                    if (ImGui.Selectable(UiNames.GetName(mode), mode == _createMode)) {
                        Service.Log.Info($"set to {mode}");
                        _createMode = mode;
                    }
                }
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Create new")) {
            Plugin.Settings.DisplayConfigs.Custom.Add(
                new DisplayConfig($"Custom {Plugin.Settings.DisplayConfigs.Custom.Count + 1}", _createMode));
            Plugin.Settings.Save();
        }

        ImGuiExt.Spacer(2);

        foreach (var statusConfig in Plugin.Settings.DisplayConfigs.Custom) {
            DrawDisplayConfig(statusConfig, ref actions);
        }

        foreach (var action in actions) {
            action();
        }
    }

    private static void DrawDisplayConfig(DisplayConfig config, ref List<Action> actions)
    {
        using var id = ImRaii.PushId($"display@{config.Preset}@{config.Id}");

        if (!ImGui.CollapsingHeader($"{UiNames.GetName(config)}###statusHeader@{config.Preset}@{config.Id}"))
            return;

        using var indent = ImRaii.PushIndent();

        if (config.Preset == DisplayPreset.Custom) {
            ImGui.TextDisabled("User-created");
            // ImGui.TextDisabled("Based on: ");
            // ImGui.SameLine();
            ImGui.TextUnformatted(UiNames.GetName(config.Mode));

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

        ImGuiExt.DrawIconSetCombo("Icon set", true, () => config.IconSetId, iconSetId =>
        {
            config.IconSetId = iconSetId;
            Plugin.Settings.Save();
        });

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

        if (config.Mode is NameplateMode.SmallJobIconAndRole or NameplateMode.RoleLetters) {
            using var combo = ImRaii.Combo("Role display style", UiNames.GetName(config.RoleDisplayStyle));
            if (combo) {
                foreach (var style in Enum.GetValues<RoleDisplayStyle>().Where(r => r != RoleDisplayStyle.None)) {
                    if (ImGui.Selectable(UiNames.GetName(style), style == config.RoleDisplayStyle)) {
                        config.RoleDisplayStyle = style;
                        Plugin.Settings.Save();
                    }
                }
            }
        }

        ImGuiExt.Spacer(6);
        ImGui.TextDisabled("Job Icon");
        using (ImRaii.PushId("jobIcon")) {
            DrawJobIcon(() => config.JobIcon, icon => config.JobIcon = icon);
        }

        ImGui.TextDisabled("Status Icon");
        using (ImRaii.PushId("statusIcon")) {
            DrawJobIcon(() => config.StatusIcon, icon => config.StatusIcon = icon);
        }

        ImGuiExt.Spacer(6);
        ImGui.TextDisabled("Status icon visibility by location");
        foreach (var zoneType in Enum.GetValues<ZoneType>()) {
            DrawStatusSelector(config, zoneType);
        }

        ImGuiExt.Spacer(6);
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

        ImGuiExt.Spacer(3);
    }

    private static void DrawJobIcon(Func<IconCustomizeConfig> getter, Action<IconCustomizeConfig> setter)
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
        if (ImGui.SliderInt("Offset X/Y", pos.AsSpan(), -50, 50)) {
            var x = (short)Math.Clamp(pos[0], -1000, 1000);
            var y = (short)Math.Clamp(pos[1], -1000, 1000);
            setter(icon with { OffsetX = x, OffsetY = y });
            Plugin.Settings.Save();
        }

        ImGuiComponents.HelpMarker("Hold Control and click a slider to input an exact value");
    }

    private static void DrawStatusSelector(DisplayConfig config, ZoneType zoneType)
    {
        var currentSelector = config.StatusSelectors[zoneType];
        ImGuiExt.SetComboWidth(Plugin.Settings.StatusConfigs.Selectors.Select(UiNames.GetName));
        using var combo = ImRaii.Combo($"{UiNames.GetName(zoneType)}##zoneSelector@{zoneType}",
            UiNames.GetName(currentSelector));
        if (!combo) return;

        foreach (var selector in Plugin.Settings.StatusConfigs.Selectors) {
            using var col = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.HealerGreen,
                selector.Preset == StatusPreset.Custom);
            if (ImGui.Selectable(UiNames.GetName(selector), currentSelector == selector)) {
                config.StatusSelectors[zoneType] = selector;
                Plugin.Settings.Save();
            }
        }
    }
}