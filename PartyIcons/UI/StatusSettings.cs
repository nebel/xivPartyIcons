using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PartyIcons.Configuration;
using PartyIcons.Utils;
using Action = System.Action;
using Status = PartyIcons.Entities.Status;

namespace PartyIcons.UI;

public sealed class StatusSettings
{
    private IDalamudTextureWrap? GetIconTexture(uint iconId)
    {
        var path = Service.TextureProvider.GetIconPath(iconId, ITextureProvider.IconFlags.None);
        if (path == null)
            return null;

        return Service.TextureProvider.GetTextureFromGame(path);
    }

    private static StatusVisibility ToggleStatusDisplay(StatusVisibility visibility)
    {
        return visibility switch
        {
            StatusVisibility.Hide => StatusVisibility.Show,
            StatusVisibility.Show => StatusVisibility.Important,
            StatusVisibility.Important => StatusVisibility.Hide,
            _ => StatusVisibility.Hide
        };
    }

    public void DrawStatusSettings()
    {
        const float separatorPadding = 2f;
        ImGui.Dummy(new Vector2(0, separatorPadding));

        ImGui.TextDisabled("Configure status icon visibility based on location");
        ImGui.Dummy(new Vector2(0, separatorPadding));

        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.Text("Presets");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 2f));

        List<Action> actions = [];

        DrawStatusConfig(Plugin.Settings.StatusConfigs.Overworld, ref actions);
        DrawStatusConfig(Plugin.Settings.StatusConfigs.Instances, ref actions);
        DrawStatusConfig(Plugin.Settings.StatusConfigs.FieldOperations, ref actions);

        ImGui.Dummy(new Vector2(0, 15f));
        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.Text("User-created");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 2f));

        if (ImGui.Button("Create new")) {
            Plugin.Settings.StatusConfigs.Custom.Add(
                new StatusConfig($"Custom status list {Plugin.Settings.StatusConfigs.Custom.Count + 1}"));
            Plugin.Settings.Save();
        }

        foreach (var statusConfig in Plugin.Settings.StatusConfigs.Custom) {
            DrawStatusConfig(statusConfig, ref actions);
        }

        foreach (var action in actions) {
            action();
        }
    }

    private void DrawStatusConfig(StatusConfig config, ref List<Action> actions)
    {
        var textSize = ImGui.CalcTextSize("Important");
        var rowHeight = textSize.Y + ImGui.GetStyle().FramePadding.Y * 2;
        var iconSize = new Vector2(rowHeight, rowHeight);
        var buttonSize = new Vector2(textSize.X + ImGui.GetStyle().FramePadding.X * 2 + 10, rowHeight);
        var buttonXAdjust = -(ImGui.GetStyle().ScrollbarSize + ImGui.GetStyle().WindowPadding.X + buttonSize.X);

        var sheet = Service.DataManager.GameData.GetExcelSheet<OnlineStatus>()!;

        using (ImRaii.PushId($"status@{config.Preset}@{config.Id}")) {
            if (!ImGui.CollapsingHeader($"{SettingsWindow.GetName(config)}###statusHeader@{config.Preset}@{config.Id}")) return;

            using (ImRaii.PushIndent(iconSize.X + ImGui.GetStyle().FramePadding.X + ImGui.GetStyle().ItemSpacing.X)) {
                if (config.Preset == StatusPreset.Custom) {
                    ImGui.TextDisabled("Name: ");

                    ImGui.SameLine();
                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 10);
                    var name = config.Name ?? "";
                    if (ImGui.InputText("##rename", ref name, 100, ImGuiInputTextFlags.EnterReturnsTrue)) {
                        actions.Add(() =>
                        {
                            config.Name = name.Replace("%", "");
                            Plugin.Settings.Save();
                        });
                    }
                    ImGui.PopItemWidth();
                }

                ImGui.TextDisabled("Other actions: ");
                if (config.Preset != StatusPreset.Custom) {
                    ImGui.SameLine();
                    if (ImGuiExt.ButtonEnabledWhen(ImGui.GetIO().KeyCtrl, "Reset to default")) {
                        config.Reset();
                        Plugin.Settings.Save();
                    }

                    ImGuiExt.HoverTooltip("Hold Control to allow reset");
                }
                else {
                    ImGui.SameLine();
                    if (ImGuiExt.ButtonEnabledWhen(ImGui.GetIO().KeyCtrl, "Delete")) {
                        actions.Add(() =>
                        {
                            Plugin.Settings.DisplayConfigs.RemoveSelectors(config);
                            Plugin.Settings.StatusConfigs.Custom.RemoveAll(c => c.Id == config.Id);
                            Plugin.Settings.Save();
                        });
                    }

                    ImGuiExt.HoverTooltip("Hold Control to allow deletion");
                }

                ImGui.SameLine();
                if (ImGui.Button("Copy to new list")) {
                    actions.Add(() =>
                    {
                        Plugin.Settings.StatusConfigs.Custom.Add(new StatusConfig(
                            $"{SettingsWindow.GetName(config)} ({Plugin.Settings.StatusConfigs.Custom.Count + 1})", config));
                        Plugin.Settings.Save();
                    });
                }
            }

            Status? clicked = null;
            foreach (var status in StatusUtils.ConfigurableStatuses) {
                var display = config.DisplayMap.GetValueOrDefault(status, StatusVisibility.Hide);
                var row = sheet.GetRow((uint)status);
                if (row == null) continue;

                ImGui.Separator();

                var icon = GetIconTexture(row.Icon);
                if (icon != null) {
                    ImGui.Image(icon.ImGuiHandle, iconSize);
                    ImGui.SameLine();
                }

                using (ImRaii.PushColor(ImGuiCol.Button, 0))
                using (ImRaii.PushColor(ImGuiCol.ButtonHovered, 0))
                using (ImRaii.PushColor(ImGuiCol.ButtonActive, 0)) {
                    ImGui.Button($"{sheet.GetRow((uint)status)!.Name.RawString}##name{(int)status}");
                    ImGui.SameLine();
                }

                var color = display switch
                {
                    StatusVisibility.Hide => (0xFF555555, 0xFF666666, 0xFF777777),
                    StatusVisibility.Show => (0xFF558855, 0xFF55AA55, 0xFF55CC55),
                    StatusVisibility.Important => (0xFF5555AA, 0xFF5555CC, 0xFF5555FF),
                    _ => (0xFFAA00AA, 0xFFBB00BB, 0xFFFF00FF)
                };

                using (ImRaii.PushColor(ImGuiCol.Text, 0xFFEEEEEE))
                using (ImRaii.PushColor(ImGuiCol.Button, color.Item1))
                using (ImRaii.PushColor(ImGuiCol.ButtonHovered, color.Item2))
                using (ImRaii.PushColor(ImGuiCol.ButtonActive, color.Item3)) {
                    ImGui.SetCursorPosX(ImGui.GetWindowWidth() + buttonXAdjust);
                    if (ImGui.Button($"{display.ToString()}##toggle{(int)status}", buttonSize)) {
                        clicked = status;
                    }
                }
            }

            if (clicked is { } clickedStatus) {
                var oldState = config.DisplayMap[clickedStatus];
                var newState = ToggleStatusDisplay(oldState);
                // Service.Log.Info($"Clicked {clickedStatus}: {oldState} -> {newState}");
                config.DisplayMap[clickedStatus] = newState;
                Plugin.Settings.Save();
            }
        }
    }
}