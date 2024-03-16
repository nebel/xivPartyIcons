using System;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using PartyIcons.Api;
using PartyIcons.View;

namespace PartyIcons.Runtime;

/// <summary>
/// Reverts NPC nameplates that have had their icon or name text scaled and
/// also reverts all nameplates when the plugin is unloading.
/// </summary>
public sealed class NPCNameplateFixer : IDisposable
{
    private const uint NoTarget = 0xE0000000;
    private readonly NameplateView _view;

    public NPCNameplateFixer(NameplateView view)
    {
        _view = view;
    }

    public void Enable()
    {
        Service.Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        Service.Framework.Update -= OnUpdate;
        RevertAll();
    }

    private void OnUpdate(IFramework framework)
    {
        RevertNPC();
    }

    private void RevertNPC()
    {
        foreach (var npObject in new XivApi.NamePlateArrayReader()) {
            if (npObject is not { IsVisible: true } || npObject.IsPlayer)
            {
                continue;
            }

            if (_view.SetupDefault(npObject))
            {
                PluginLog.Information($"  -> npc reverted ({npObject.IsPlayer})"); // FIXME debugging
            }
        }
    }

    private void RevertAll()
    {
        foreach (var npObject in new XivApi.NamePlateArrayReader()) {
            _view.SetupDefault(npObject);
        }
    }
}
