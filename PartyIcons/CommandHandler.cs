using System;
using Dalamud.Game.Command;
using PartyIcons.Runtime;

namespace PartyIcons;

public class CommandHandler : IDisposable
{
    private const string commandName = "/ppi";
    
    public CommandHandler()
    {
        Service.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
        {
            HelpMessage =
                "opens configuration window; \"reset\" or \"r\" resets all assignments; \"debug\" prints debugging info"
        });
    }
    
    public void Dispose()
    {
        Service.CommandManager.RemoveHandler(commandName);
    }

    private void OnCommand(string command, string arguments)
    {
        arguments = arguments.Trim().ToLower();

        if (arguments == "" || arguments == "config")
        {
            Plugin.SettingsWindow.ToggleSettingsWindow();
        }
        else if (arguments == "reset" || arguments == "r")
        {
            Plugin.RoleTracker.ResetOccupations();
            Plugin.RoleTracker.ResetAssignments();
            Plugin.RoleTracker.CalculateUnassignedPartyRoles();
            Service.ChatGui.Print("Occupations are reset, roles are auto assigned.", Service.PluginInterface.InternalName, 45);
        }
        else if (arguments == "dbg r")
        {
            Plugin.RoleTracker.ResetOccupations();
            Plugin.RoleTracker.ResetAssignments();
            Service.ChatGui.Print("Occupations/assignments are reset.", Service.PluginInterface.InternalName, 45);
        }
        else if (arguments == "dbg state")
        {
            Service.Log.Info($"Current mode is {Plugin.NameplateView.PartyDisplay.Mode}, party count {Service.PartyList.Length}", Service.PluginInterface.InternalName, 45);
            Service.Log.Info(Plugin.RoleTracker.DebugDescription(), Service.PluginInterface.InternalName, 45);
        }
        else if (arguments == "dbg party")
        {
            PartyListHUDUpdater.DebugPartyData();
        }
    }
}