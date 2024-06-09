using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PartyIcons.Configuration;
using PartyIcons.Utils;
using Status = PartyIcons.Entities.Status;

namespace PartyIcons.UI;

public sealed class StatusSettings
{
    // private static readonly Vector2 IconSize = new(24, 24);
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

        DrawStatusConfig(Plugin.Settings.StatusSettings.Overworld);
        DrawStatusConfig(Plugin.Settings.StatusSettings.Instances);
        DrawStatusConfig(Plugin.Settings.StatusSettings.FieldOperations);
    }

    private void DrawStatusConfig(StatusConfig config)
    {
        var textSize = ImGui.CalcTextSize("Important");
        var rowHeight = textSize.Y + ImGui.GetStyle().FramePadding.Y * 2;
        var iconSize = new Vector2(rowHeight, rowHeight);
        var buttonSize = new Vector2(textSize.X + ImGui.GetStyle().FramePadding.X * 2 + 10, rowHeight);
        var buttonXAdjust = -(ImGui.GetStyle().ScrollbarSize + ImGui.GetStyle().WindowPadding.X + buttonSize.X);

        var sheet = Service.DataManager.GameData.GetExcelSheet<OnlineStatus>()!;

        using (ImRaii.PushId($"statusHeader@{config.Preset}@{config.Id}")) {
            if (!ImGui.CollapsingHeader(GetName(config))) return;

            using (ImRaii.PushIndent(15 * ImGuiHelpers.GlobalScale)) {
                var textOffset = ImGui.GetStyle().FramePadding.X / 2;
                ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + textOffset);
                if (config.Preset != StatusPreset.Custom) {
                    ImGui.TextDisabled("Other actions: ");
                    ImGui.SameLine();
                    ImGui.SetCursorPosY(ImGui.GetCursorPos().Y - textOffset);
                    if (ImGui.Button("Reset to default")) {
                        config.Reset();
                        Plugin.Settings.Save();
                    }
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

    private static string GetName(StatusConfig config)
    {
        return config.Preset switch
        {
            StatusPreset.Custom => config.Name,
            StatusPreset.Overworld => "Overworld",
            StatusPreset.Instances => "Instances",
            StatusPreset.FieldOperations => "Field Operations",
            _ => config.Name
        };
    }
}