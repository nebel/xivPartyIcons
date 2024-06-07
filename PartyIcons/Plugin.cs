using Dalamud.Plugin;
using PartyIcons.Configuration;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using PartyIcons.UI;
using PartyIcons.Utils;
using PartyIcons.View;

namespace PartyIcons;

public sealed class Plugin : IDalamudPlugin
{
    public static PartyListHUDView PartyHudView { get; private set; } = null!;
    public static PartyListHUDUpdater PartyListHudUpdater { get; private set; } = null!;
    public static SettingsWindow SettingsWindow { get; private set; } = null!;
    public static NameplateUpdater NameplateUpdater { get; private set; } = null!;
    public static NameplateView NameplateView { get; private set; } = null!;
    public static RoleTracker RoleTracker { get; private set; } = null!;
    public static ViewModeSetter ModeSetter { get; private set; } = null!;
    public static ChatNameUpdater ChatNameUpdater { get; private set; } = null!;
    public static ContextMenu ContextMenu { get; private set; } = null!;
    public static CommandHandler CommandHandler { get; private set; } = null!;
    public static Settings Settings { get; private set; } = null!;
    public static PlayerStylesheet PlayerStylesheet { get; private set; } = null!;
    public static StatusResolver StatusResolver { get; private set; } = null!;

    public Plugin(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        Settings = Settings.Load();

        PlayerStylesheet = new PlayerStylesheet(Settings);

        SettingsWindow = new SettingsWindow();

        SeStringUtils.Initialize();

        PartyHudView = new PartyListHUDView(PlayerStylesheet);
        RoleTracker = new RoleTracker(Settings);
        StatusResolver = new StatusResolver(Settings);
        NameplateView = new NameplateView(RoleTracker, Settings, PlayerStylesheet, StatusResolver);
        ChatNameUpdater = new ChatNameUpdater(RoleTracker, PlayerStylesheet);
        PartyListHudUpdater = new PartyListHUDUpdater(PartyHudView, RoleTracker, Settings);
        ModeSetter = new ViewModeSetter(NameplateView, Settings, ChatNameUpdater, PartyListHudUpdater, StatusResolver);
        NameplateUpdater = new NameplateUpdater(NameplateView);
        ContextMenu = new ContextMenu(RoleTracker, Settings, PlayerStylesheet);
        CommandHandler = new CommandHandler();

        SettingsWindow.Initialize();

        PartyListHudUpdater.Enable();
        ModeSetter.Enable();
        RoleTracker.Enable();
        NameplateUpdater.Enable();
        ChatNameUpdater.Enable();
    }

    public void Dispose()
    {
        PartyHudView.Dispose();
        PartyListHudUpdater.Dispose();
        ChatNameUpdater.Dispose();
        ContextMenu.Dispose();
        NameplateUpdater.Dispose();
        RoleTracker.Dispose();
        ModeSetter.Dispose();
        SettingsWindow.Dispose();
        CommandHandler.Dispose();

        SeStringUtils.Dispose();
    }
}