﻿using Dalamud.Plugin;
using PartyIcons.Configuration;
using PartyIcons.Dalamud;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using PartyIcons.UI;
using PartyIcons.Utils;
using PartyIcons.View;

namespace PartyIcons;

public sealed class Plugin : IDalamudPlugin
{
    public static PartyStateTracker PartyStateTracker { get; private set; } = null!;
    public static PartyListHUDView PartyHudView { get; private set; } = null!;
    public static PartyListHUDUpdater PartyListHudUpdater { get; private set; } = null!;
    public static SettingsWindow SettingsWindow { get; private set; } = null!;
    // public static NameplateUpdater NameplateUpdater { get; private set; } = null!;
    public static NameplateUpdater2 NameplateUpdater2 { get; private set; } = null!;
    public static NameplateView NameplateView { get; private set; } = null!;
    public static RoleTracker RoleTracker { get; private set; } = null!;
    public static ViewModeSetter ModeSetter { get; private set; } = null!;
    public static ChatNameUpdater ChatNameUpdater { get; private set; } = null!;
    public static ContextMenu ContextMenu { get; private set; } = null!;
    public static CommandHandler CommandHandler { get; private set; } = null!;
    public static Settings Settings { get; private set; } = null!;
    public static PlayerStylesheet PlayerStylesheet { get; private set; } = null!;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        Service.NamePlateGui = new NamePlateGui();

        Settings = Settings.Load();

        PlayerStylesheet = new PlayerStylesheet(Settings);

        SettingsWindow = new SettingsWindow();

        SeStringUtils.Initialize();

        PartyStateTracker = new PartyStateTracker();
        PartyHudView = new PartyListHUDView(PlayerStylesheet);
        RoleTracker = new RoleTracker(Settings, PartyStateTracker);
        NameplateView = new NameplateView(RoleTracker, Settings, PlayerStylesheet);
        ChatNameUpdater = new ChatNameUpdater(RoleTracker, PlayerStylesheet);
        PartyListHudUpdater = new PartyListHUDUpdater(PartyHudView, RoleTracker, Settings, PartyStateTracker);
        ModeSetter = new ViewModeSetter(NameplateView, Settings, ChatNameUpdater, PartyListHudUpdater);
        // NameplateUpdater = new NameplateUpdater(NameplateView);
        NameplateUpdater2 = new NameplateUpdater2(NameplateView);
        ContextMenu = new ContextMenu(RoleTracker, Settings, PlayerStylesheet);
        CommandHandler = new CommandHandler();

        SettingsWindow.Initialize();

        PartyStateTracker.Enable();
        PartyListHudUpdater.Enable();
        ModeSetter.Enable();
        RoleTracker.Enable();
        // NameplateUpdater.Enable();
        NameplateUpdater2.Enable();
        ChatNameUpdater.Enable();

        Service.NamePlateGui.Enable();
    }

    public void Dispose()
    {
        PartyStateTracker.Dispose();
        PartyHudView.Dispose();
        PartyListHudUpdater.Dispose();
        ChatNameUpdater.Dispose();
        ContextMenu.Dispose();
        // NameplateUpdater.Dispose();
        NameplateUpdater2.Dispose();
        RoleTracker.Dispose();
        ModeSetter.Dispose();
        SettingsWindow.Dispose();
        CommandHandler.Dispose();
        Service.NamePlateGui.Dispose();

        SeStringUtils.Dispose();
    }
}