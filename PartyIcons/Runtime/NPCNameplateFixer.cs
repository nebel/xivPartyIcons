using System;
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
        var reader = new NamePlateArrayReader();
        if (!reader.HasValidPointer())
            return;

        for (var i = 0; i < NamePlateArrayReader.NumNameplates; i++) {
            if (reader.GetUnchecked(i) is { IsVisible: true, IsPlayer: false } npObject) {
                _view.SetupDefault(npObject);
            }
        }
    }

    private void RevertAll()
    {
        var reader = new NamePlateArrayReader();
        if (!reader.HasValidPointer())
            return;

        for (var i = 0; i < NamePlateArrayReader.NumNameplates; i++) {
            _view.SetupDefault(reader.GetUnchecked(i));
        }
    }
}
