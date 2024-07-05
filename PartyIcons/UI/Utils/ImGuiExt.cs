using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using ImGuiNET;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.Stylesheet;
using PartyIcons.View;

namespace PartyIcons.UI.Utils;

public static class ImGuiExt
{
    public static bool ButtonEnabledWhen(bool enabled, string text)
    {
        if (!enabled)
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
        var result = ImGui.Button(text);
        if (!enabled)
            ImGui.PopStyleVar();

        return result && enabled;
    }

    public static void HoverTooltip(string text)
    {
        if (!ImGui.IsItemHovered()) {
            return;
        }

        ImGui.BeginTooltip();
        ImGui.TextUnformatted(text);
        ImGui.EndTooltip();
    }

    public static void Spacer(int size)
    {
        ImGui.Dummy(new Vector2(0, size));
    }

    public static void SectionHeader(string text)
    {
        ImGui.Dummy(new Vector2(0, 2f));
        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 2f));
    }

    public static void SetComboWidth(IEnumerable<string> values)
    {
        const float paddingMultiplier = 1.05f;
        var maxItemWidth = values
            .Select(text => ImGui.CalcTextSize(text).X + ImGui.GetStyle().ScrollbarSize * 3f)
            .Append(50)
            .Max();

        ImGui.SetNextItemWidth(maxItemWidth * paddingMultiplier);
    }

    public static ISharedImmediateTexture GetIconTexture(uint iconId)
    {
        var path = Service.TextureProvider.GetIconPath(new GameIconLookup(iconId));
        return Service.TextureProvider.GetFromGame(path);
    }

    public static void DrawIconSetCombo(string label, bool showInherit, Func<IconSetId> getter, Action<IconSetId> setter)
    {
        var currentIconSetId = getter();
        var iconSetIds = Enum.GetValues<IconSetId>().Where(id => id != IconSetId.Inherit).ToList();
        if (showInherit) {
            iconSetIds.Insert(0, IconSetId.Inherit);
        }

        using (var combo = ImRaii.Combo(label, UiNames.GetName(currentIconSetId))) {
            if (combo) {
                foreach (var id in iconSetIds) {
                    if (ImGui.Selectable($"{UiNames.GetName(id)}##{label}_{id}")) {
                        setter(id);
                    }
                }
            }
        }

        if (currentIconSetId != IconSetId.Inherit && Service.ClientState.LocalPlayer is { } player) {
            var job = (Job)player.ClassJob.Id;
            var iconGroupId = PlayerStylesheet.GetGenericRoleIconGroupId(currentIconSetId, job.GetRole());
            var iconGroup = IconRegistrar.Get(iconGroupId);
            var iconId = iconGroup.GetJobIcon((uint)job);
            var icon = GetIconTexture(iconId).GetWrapOrDefault();
            if (icon != null) {
                var textSize = ImGui.CalcTextSize("Important");
                var imageSize = textSize.Y + ImGui.GetStyle().FramePadding.Y * 2;

                var scale = iconGroup.Scale;
                var b = (icon.Width * scale - icon.Width) / 2;
                var uv0 = new Vector2(b, b) / icon.Size;
                var uv1 = new Vector2(icon.Width - b, icon.Height - b) / icon.Size;
                // Service.Log.Warning($"uv0 {uv0} uv1 {uv1}");

                ImGui.SameLine();
                ImGui.Image(icon.ImGuiHandle, new Vector2(imageSize, imageSize), uv0, uv1);
            }
        }
    }
}