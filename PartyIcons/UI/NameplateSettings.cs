﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using ImGuiNET;
using PartyIcons.Configuration;

namespace PartyIcons.UI;

public sealed class NameplateSettings
{
    private readonly Dictionary<NameplateMode, IDalamudTextureWrap> _nameplateExamples = new();

    public void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var examplesImageNames = new Dictionary<NameplateMode, string>
        {
            {NameplateMode.SmallJobIcon, "PartyIcons.Resources.1.png"},
            {NameplateMode.BigJobIcon, "PartyIcons.Resources.2.png"},
            {NameplateMode.BigJobIconAndPartySlot, "PartyIcons.Resources.3.png"},
            {NameplateMode.RoleLetters, "PartyIcons.Resources.4.png"}
        };

        foreach (var kv in examplesImageNames)
        {
            using var fileStream = assembly.GetManifestResourceStream(kv.Value);

            if (fileStream == null)
            {
                Service.Log.Error($"Failed to get resource stream for {kv.Value}");

                continue;
            }

            using var memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);

            _nameplateExamples[kv.Key] = Service.PluginInterface.UiBuilder.LoadImage(memoryStream.ToArray());
        }
    }

    public void DrawNameplateSettings()
    {
        const float separatorPadding = 2f;

        ImGui.Dummy(new Vector2(0, 2f));
        var iconSetId = Plugin.Settings.IconSetId;
        ImGui.Text("Icon set:");
        ImGui.SameLine();
        SettingsWindow.SetComboWidth(Enum.GetValues<IconSetId>().Select(IconSetIdToString));

        if (ImGui.BeginCombo("##icon_set", IconSetIdToString(iconSetId)))
        {
            foreach (var id in Enum.GetValues<IconSetId>())
            {
                if (ImGui.Selectable(IconSetIdToString(id) + "##icon_set_" + id))
                {
                    Plugin.Settings.IconSetId = id;
                    Plugin.Settings.Save();
                }
            }

            ImGui.EndCombo();
        }

        var iconSizeMode = Plugin.Settings.SizeMode;
        ImGui.Text("Nameplate size:");
        ImGui.SameLine();
        SettingsWindow.SetComboWidth(Enum.GetValues<NameplateSizeMode>().Select(x => x.ToString()));

        if (ImGui.BeginCombo("##icon_size", iconSizeMode.ToString()))
        {
            foreach (var mode in Enum.GetValues<NameplateSizeMode>())
            {
                if (ImGui.Selectable(mode + "##icon_set_" + mode))
                {
                    Plugin.Settings.SizeMode = mode;
                    Plugin.Settings.Save();
                }
            }

            ImGui.EndCombo();
        }

        SettingsWindow.ImGuiHelpTooltip("Affects all presets, except Game Default and Small Job Icon.");

        var hideLocalNameplate = Plugin.Settings.HideLocalPlayerNameplate;

        if (ImGui.Checkbox("##hidelocal", ref hideLocalNameplate))
        {
            Plugin.Settings.HideLocalPlayerNameplate = hideLocalNameplate;
            Plugin.Settings.Save();
        }

        ImGui.SameLine();
        ImGui.Text("Hide own nameplate");
        SettingsWindow.ImGuiHelpTooltip(
            "You can turn your own nameplate on and also turn this\nsetting own to only use nameplate to display own raid position.\nIf you don't want your position displayed with this setting you can simply disable\nyour nameplates in the Character settings.");

        ImGui.Dummy(new Vector2(0f, 10f));

        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.Text("Overworld");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, separatorPadding));
        ImGui.Indent(15 * ImGuiHelpers.GlobalScale);
        {
            NameplateModeSection("##np_overworld", () => Plugin.Settings.DisplaySelections.DisplayOverworld,
                sel => Plugin.Settings.DisplaySelections.DisplayOverworld = sel,
                "Party:");

            NameplateModeSection("##np_others", () => Plugin.Settings.DisplaySelections.DisplayOthers,
                sel => Plugin.Settings.DisplaySelections.DisplayOthers = sel,
                "Others:");
        }
        ImGui.Indent(-15 * ImGuiHelpers.GlobalScale);
        ImGui.Dummy(new Vector2(0, 2f));

        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.Text("Instances");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, separatorPadding));
        ImGui.Indent(15 * ImGuiHelpers.GlobalScale);
        {
            NameplateModeSection("##np_dungeon", () => Plugin.Settings.DisplaySelections.DisplayDungeon,
                (sel) => Plugin.Settings.DisplaySelections.DisplayDungeon = sel,
                "Dungeon:");

            NameplateModeSection("##np_raid", () => Plugin.Settings.DisplaySelections.DisplayRaid,
                sel => Plugin.Settings.DisplaySelections.DisplayRaid = sel,
                "Raid:");

            NameplateModeSection("##np_alliance", () => Plugin.Settings.DisplaySelections.DisplayAllianceRaid,
                sel => Plugin.Settings.DisplaySelections.DisplayAllianceRaid = sel,
                "Alliance:");
        }
        ImGui.Indent(-15 * ImGuiHelpers.GlobalScale);
        ImGui.Dummy(new Vector2(0, 2f));

        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.Text("Field Operations");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, separatorPadding));
        ImGui.Indent(15 * ImGuiHelpers.GlobalScale);
        {
            ImGui.TextDisabled("e.g. Eureka, Bozja");

            NameplateModeSection("##np_field_party", () => Plugin.Settings.DisplaySelections.DisplayFieldOperationParty,
                sel => Plugin.Settings.DisplaySelections.DisplayFieldOperationParty = sel, "Party:");

            NameplateModeSection("##np_field_others", () => Plugin.Settings.DisplaySelections.DisplayFieldOperationOthers,
                sel => Plugin.Settings.DisplaySelections.DisplayFieldOperationOthers = sel, "Others:");
        }
        ImGui.Indent(-15 * ImGuiHelpers.GlobalScale);
        ImGui.Dummy(new Vector2(0, 2f));

        ImGui.PushStyleColor(0, ImGuiHelpers.DefaultColorPalette()[0]);
        ImGui.Text("PvP");
        ImGui.PopStyleColor();
        ImGui.Separator();
        ImGui.Dummy(new Vector2(0, 2f));
        ImGui.Indent(15 * ImGuiHelpers.GlobalScale);
        {
            ImGui.TextDisabled("This plugin is intentionally disabled during PvP matches.");
        }
        ImGui.Indent(-15 * ImGuiHelpers.GlobalScale);
        ImGui.Dummy(new Vector2(0, 10f));

        ImGui.Dummy(new Vector2(0, 10f));

        if (ImGui.CollapsingHeader("Examples"))
        {
            foreach (var kv in _nameplateExamples)
            {
                CollapsibleExampleImage(kv.Key, kv.Value);
            }
        }
    }

    private static void CollapsibleExampleImage(NameplateMode mode, IDalamudTextureWrap tex)
    {
        if (ImGui.CollapsingHeader(SettingsWindow.GetName(mode)))
        {
            ImGui.Image(tex.ImGuiHandle, new Vector2(tex.Width, tex.Height));
        }
    }

    private static string IconSetIdToString(IconSetId id)
    {
        return id switch
        {
            IconSetId.EmbossedFramed => "Framed, role colored",
            IconSetId.EmbossedFramedSmall => "Framed, role colored (small)",
            IconSetId.Gradient => "Gradient, role colored",
            IconSetId.Glowing => "Glowing",
            IconSetId.Embossed => "Embossed",
            _ => id.ToString()
        };
    }

    private static void NameplateModeSection(string label, Func<DisplaySelector> getter, Action<DisplaySelector> setter, string title = "Nameplate: ")
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPos().Y + 3f);
        ImGui.Text(title);
        ImGui.SameLine(100f);
        ImGui.SetCursorPosY(ImGui.GetCursorPos().Y - 3f);
        SettingsWindow.SetComboWidth(Plugin.Settings.DisplayConfigs.Selectors.Select(SettingsWindow.GetName));

        // hack to fix incorrect configurations
        // try
        // {
        //     getter();
        // }
        // catch (ArgumentException ex)
        // {
        //     setter(new DisplaySelector(DisplayPreset.Default));
        //     Plugin.Settings.Save();
        // }

        if (ImGui.BeginCombo(label, SettingsWindow.GetName(getter())))
        {
            foreach (var selector in Plugin.Settings.DisplayConfigs.Selectors) {
                if (selector.Preset == DisplayPreset.Custom) {
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.HealerGreen);
                }
                if (ImGui.Selectable(SettingsWindow.GetName(selector), selector == getter()))
                {
                    setter(selector);
                    Plugin.Settings.Save();
                }
                if (selector.Preset == DisplayPreset.Custom) {
                    ImGui.PopStyleColor();
                }
            }

            ImGui.EndCombo();
        }
    }
}