using ImGuiNET;

namespace PartyIcons.Utils;

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
}