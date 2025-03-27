using Dalamud.Plugin;
using PartyIcons.Configuration;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using PartyIcons.UI;
using PartyIcons.View;

namespace PartyIcons;

public sealed class Plugin : IDalamudPlugin
{
    public static PartyStateTracker PartyStateTracker { get; private set; } = null!;
    public static PartyListHUDUpdater PartyListHudUpdater { get; private set; } = null!;
    public static NameplateUpdater NameplateUpdater { get; private set; } = null!;
    public static NameplateView NameplateView { get; private set; } = null!;
    public static RoleTracker RoleTracker { get; private set; } = null!;
    public static ViewModeSetter ModeSetter { get; private set; } = null!;
    public static ChatNameUpdater ChatNameUpdater { get; private set; } = null!;
    public static ContextMenu ContextMenu { get; private set; } = null!;
    public static CommandHandler CommandHandler { get; private set; } = null!;
    public static Settings Settings { get; private set; } = null!;
    public static PlayerStylesheet PlayerStylesheet { get; private set; } = null!;
    public static WindowManager WindowManager { get; private set; } = null!;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        Settings = Settings.Load();

        PlayerStylesheet = new PlayerStylesheet(Settings);

        PartyStateTracker = new PartyStateTracker();
        RoleTracker = new RoleTracker(Settings, PartyStateTracker);
        NameplateView = new NameplateView(RoleTracker, Settings, PlayerStylesheet);
        ChatNameUpdater = new ChatNameUpdater(RoleTracker, PlayerStylesheet);
        PartyListHudUpdater = new PartyListHUDUpdater(RoleTracker, Settings);
        ModeSetter = new ViewModeSetter(NameplateView, Settings, ChatNameUpdater, PartyListHudUpdater);
        NameplateUpdater = new NameplateUpdater(NameplateView);
        ContextMenu = new ContextMenu(RoleTracker, Settings, PlayerStylesheet);
        CommandHandler = new CommandHandler();
        WindowManager = new WindowManager();

        Service.Framework.RunOnFrameworkThread(() =>
        {
            PartyListHudUpdater.Enable();
            ModeSetter.Enable();
            RoleTracker.Enable();
            NameplateUpdater.Enable();
            ChatNameUpdater.Enable();
            PartyStateTracker.Enable();
        });
    }

    public void Dispose()
    {
        Service.NamePlateGui.RequestRedraw();

        PartyStateTracker.Dispose();
        PartyListHudUpdater.Dispose();
        ChatNameUpdater.Dispose();
        ContextMenu.Dispose();
        NameplateUpdater.Dispose();
        RoleTracker.Dispose();
        ModeSetter.Dispose();
        CommandHandler.Dispose();
        WindowManager.Dispose();
    }
}