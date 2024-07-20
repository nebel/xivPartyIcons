using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PartyIcons.Configuration;
using PartyIcons.UI.Utils;

namespace PartyIcons.UI;

public sealed class NameplateSettings
{
    private readonly Dictionary<NameplateMode, IDalamudTextureWrap> _nameplateExamples = new();

    public void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var examplesImageNames = new Dictionary<NameplateMode, string>
        {
            { NameplateMode.SmallJobIcon, "PartyIcons.Resources.1.png" },
            { NameplateMode.BigJobIcon, "PartyIcons.Resources.2.png" },
            { NameplateMode.BigJobIconAndPartySlot, "PartyIcons.Resources.3.png" },
            { NameplateMode.RoleLetters, "PartyIcons.Resources.4.png" }
        };

        foreach (var kv in examplesImageNames) {
            using var fileStream = assembly.GetManifestResourceStream(kv.Value);

            if (fileStream == null) {
                Service.Log.Error($"Failed to get resource stream for {kv.Value}");

                continue;
            }

            _nameplateExamples[kv.Key] = Service.TextureProvider.CreateFromImageAsync(fileStream).Result;
        }
    }

    public void Draw()
    {
        ImGui.Dummy(new Vector2(0, 2f));

        ImGui.Text("Icon set:");
        ImGui.SameLine();
        ImGuiExt.SetComboWidth(Enum.GetValues<IconSetId>().Select(UiNames.GetName));

        ImGuiExt.DrawIconSetCombo("##icon_set", false, () => Plugin.Settings.IconSetId, iconSetId =>
        {
            Plugin.Settings.IconSetId = iconSetId;
            Plugin.Settings.Save();
        });

        var iconSizeMode = Plugin.Settings.SizeMode;
        ImGui.Text("Nameplate size:");
        ImGui.SameLine();
        ImGuiExt.SetComboWidth(Enum.GetValues<NameplateSizeMode>().Select(x => x.ToString()));

        using (var combo = ImRaii.Combo("##icon_size", iconSizeMode.ToString())) {
            if (combo) {
                foreach (var mode in Enum.GetValues<NameplateSizeMode>()) {
                    if (ImGui.Selectable(mode + "##icon_set_" + mode)) {
                        Plugin.Settings.SizeMode = mode;
                        Plugin.Settings.Save();
                    }
                }
            }
        }

        ImGuiComponents.HelpMarker("Affects all presets, except Game Default.");

        if (Plugin.Settings.SizeMode == NameplateSizeMode.Custom) {
            var scale = Plugin.Settings.SizeModeCustom;
            if (ImGui.SliderFloat("Custom scale", ref scale, 0.3f, 3f)) {
                Plugin.Settings.SizeModeCustom = Math.Clamp(scale, 0.1f, 10f);
                Plugin.Settings.Save();
            }

            ImGuiComponents.HelpMarker("Hold Control and click the slider to input an exact value");
        }

        var hideLocalNameplate = Plugin.Settings.HideLocalPlayerNameplate;
        if (ImGui.Checkbox("##hidelocal", ref hideLocalNameplate)) {
            Plugin.Settings.HideLocalPlayerNameplate = hideLocalNameplate;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Hide own nameplate");
        ImGuiComponents.HelpMarker(
            "You can turn your own nameplate on and also turn this\nsetting own to only use nameplate to display own raid position.\nIf you don't want your position displayed with this setting you can simply disable\nyour nameplates in the Character settings.");

        ImGuiExt.Spacer(6);

        ImGuiExt.SectionHeader("Overworld");
        using (ImRaii.PushIndent(15f)) {
            NameplateModeSection("##np_overworld", () => Plugin.Settings.DisplaySelectors.DisplayOverworld,
                sel => Plugin.Settings.DisplaySelectors.DisplayOverworld = sel,
                "Party:");

            NameplateModeSection("##np_others", () => Plugin.Settings.DisplaySelectors.DisplayOthers,
                sel => Plugin.Settings.DisplaySelectors.DisplayOthers = sel,
                "Others:");
        }

        ImGuiExt.SectionHeader("Instances");
        using (ImRaii.PushIndent(15f)) {
            NameplateModeSection("##np_dungeon", () => Plugin.Settings.DisplaySelectors.DisplayDungeon,
                (sel) => Plugin.Settings.DisplaySelectors.DisplayDungeon = sel,
                "Dungeon:");

            NameplateModeSection("##np_raid", () => Plugin.Settings.DisplaySelectors.DisplayRaid,
                sel => Plugin.Settings.DisplaySelectors.DisplayRaid = sel,
                "Raid:");

            NameplateModeSection("##np_alliance", () => Plugin.Settings.DisplaySelectors.DisplayAllianceRaid,
                sel => Plugin.Settings.DisplaySelectors.DisplayAllianceRaid = sel,
                "Alliance:");
        }

        ImGuiExt.SectionHeader("Field Operations");

        using (ImRaii.PushIndent(15f)) {
            ImGui.TextDisabled("e.g. Eureka, Bozja");

            NameplateModeSection("##np_field_party", () => Plugin.Settings.DisplaySelectors.DisplayFieldOperationParty,
                sel => Plugin.Settings.DisplaySelectors.DisplayFieldOperationParty = sel, "Party:");

            NameplateModeSection("##np_field_others",
                () => Plugin.Settings.DisplaySelectors.DisplayFieldOperationOthers,
                sel => Plugin.Settings.DisplaySelectors.DisplayFieldOperationOthers = sel, "Others:");
        }

        ImGuiExt.SectionHeader("PvP");

        using (ImRaii.PushIndent(15f)) {
            ImGui.TextDisabled("This plugin is intentionally disabled during PvP matches.");
        }

        ImGuiExt.Spacer(15);

        if (ImGui.CollapsingHeader("Examples")) {
            foreach (var kv in _nameplateExamples) {
                CollapsibleExampleImage(kv.Key, kv.Value);
            }
        }
    }

    private static void CollapsibleExampleImage(NameplateMode mode, IDalamudTextureWrap tex)
    {
        if (ImGui.CollapsingHeader(UiNames.GetName(mode))) {
            ImGui.Image(tex.ImGuiHandle, new Vector2(tex.Width, tex.Height));
        }
    }

    private static void NameplateModeSection(string label, Func<DisplaySelector> getter, Action<DisplaySelector> setter,
        string title = "Nameplate: ")
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + 3f);
        ImGui.Text(title);
        ImGui.SameLine(100f);
        ImGui.SetCursorPosY(ImGui.GetCursorPos().Y - 3f);
        ImGuiExt.SetComboWidth(Plugin.Settings.DisplayConfigs.Selectors.Select(UiNames.GetName));

        using var combo = ImRaii.Combo(label, UiNames.GetName(getter()));
        if (!combo) return;

        foreach (var selector in Plugin.Settings.DisplayConfigs.Selectors) {
            using var col = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.HealerGreen,
                selector.Preset == DisplayPreset.Custom);
            if (ImGui.Selectable(UiNames.GetName(selector), selector == getter())) {
                setter(selector);
                Plugin.Settings.Save();
            }
        }
    }
}