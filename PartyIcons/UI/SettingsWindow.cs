using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using PartyIcons.UI.Settings;
using PartyIcons.UI.Utils;
using System.Numerics;

namespace PartyIcons.UI;

public sealed class SettingsWindow : Window
{
    private readonly GeneralTab _generalTab = new();
    private readonly NameplateTab _nameplateTab = new();
    private readonly AppearanceTab _appearanceTab = new();
    private readonly StaticAssignmentsTab _staticAssignmentsTab = new();

    private readonly FlashingText _flashingText = new();

    public SettingsWindow() : base("PartyIcons")
    {
        Size = new Vector2(450, 600);
        SizeCondition = ImGuiCond.FirstUseEver;

        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(450, 350),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw()
    {
        if (!Plugin.Settings.SelectorsDialogComplete || UpgradeGuideTab.ForceRedisplay) {
            UpgradeGuideTab.Draw();
            return;
        }

        using var tabBar = ImRaii.TabBar("##tabbar");
        if (tabBar) {
            var text = _flashingText.PushColor(Plugin.Settings.TestingMode);
            using (var tab = ImRaii.TabItem("General")) {
                text.Dispose();
                if (tab) {
                    using var contents = ImRaii.Child("##general_content");
                    _generalTab.Draw();
                }
            }
            using (var tab = ImRaii.TabItem("Nameplates")) {
                if (tab) {
                    using var contents = ImRaii.Child("##nameplates_content");
                    _nameplateTab.Draw();
                }
            }
            using (var tab = ImRaii.TabItem("Appearance")) {
                if (tab) {
                    using var contents = ImRaii.Child("##appearance_content");
                    _appearanceTab.Draw();
                }
            }
            using (var tab = ImRaii.TabItem("Status Icons")) {
                if (tab) {
                    using var contents = ImRaii.Child("##statuses_content");
                    StatusTab.Draw();
                }
            }
            using (var tab = ImRaii.TabItem("Chat Names")) {
                if (tab) {
                    using var contents = ImRaii.Child("##chat_names_content");
                    ChatNameTab.Draw();
                }
            }
            using (var tab = ImRaii.TabItem("Roles")) {
                if (tab) {
                    using var contents = ImRaii.Child("##static_assignments_content");
                    _staticAssignmentsTab.Draw();
                }
            }
        }
    }
}