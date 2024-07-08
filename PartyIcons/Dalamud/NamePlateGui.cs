using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PartyIcons.Dalamud;

public class NamePlateGui : IDisposable
{
    public void Enable()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "NamePlate", OnPreRequestedUpdate);
    }

    private PlateUpdateContext? updateContext;
    private PlateUpdateHandler[] updateHandlers = [];
    private PlateUpdateHandler[] changedUpdateHandlers = [];

    private void CreateHandlers(PlateUpdateContext context)
    {
        var handlers = new List<PlateUpdateHandler>();
        for (var i = 0; i < AddonNamePlate.NumNamePlateObjects; i++) {
            handlers.Add(new PlateUpdateHandler(context, i));
        }
        updateHandlers = handlers.ToArray();
    }

    private void UpdateContextAndHandlers(AddonRequestedUpdateArgs args)
    {
        updateContext!.ResetState(args);
    }

    public event OnPlateUpdateDelegate? OnChangedPlatesPreUpdate;

    public event OnPlateUpdateDelegate? OnAllPlatesPreUpdate;

    public delegate void OnPlateUpdateDelegate(PlateUpdateContext context, ReadOnlySpan<PlateUpdateHandler> handlers);

    private void OnPreRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        var reqArgs = (AddonRequestedUpdateArgs)args;
        if (updateContext == null) {
            updateContext = new PlateUpdateContext(reqArgs);
            CreateHandlers(updateContext);
        }
        else {
            updateContext.ResetState(reqArgs);
        }

        var numPlatesShown = updateContext.ActiveNamePlateCount;
        var updatingPlates = new List<PlateUpdateHandler>();
        for (var i = 0; i < numPlatesShown; i++) {
            var handler = updateHandlers[i];
            handler.ResetState();

            var isUpdating = handler.IsUpdating;
            if (isUpdating) {
                updatingPlates.Add(handler);
            }

            DoDebugStuff(handler);
        }

        OnAllPlatesPreUpdate?.Invoke(updateContext, updateHandlers.AsSpan(0, numPlatesShown));

        if (updateContext.IsFullUpdate) {
            OnChangedPlatesPreUpdate?.Invoke(updateContext, updateHandlers.AsSpan(0, numPlatesShown));
        }
        else if (updatingPlates.Count != 0) {
            OnChangedPlatesPreUpdate?.Invoke(updateContext, updatingPlates.ToArray().AsSpan());
        }
    }

    private static readonly bool DebugUpdates = false;

    private void DoDebugStuff(PlateUpdateHandler handler)
    {
        if (DebugUpdates) {
            // handler.SetStringValue(NamePlateStringField.Name, "");
            // handler.SetStringValue(NamePlateStringField.FreeCompanyTag, "");
            // handler.SetStringValue(NamePlateStringField.StatusPrefix, "");
            // handler.SetNumberValue(NamePlateNumberField.Icon, 0);
            if (handler.IsUpdating || updateContext!.IsFullUpdate) {
                handler.MarkerIconId = 66181 + (int)handler.NamePlateKind;
            }
            else {
                handler.MarkerIconId = 66161 + (int)handler.NamePlateKind;
            }
        }
    }

    public void Dispose()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "NamePlate", OnPreRequestedUpdate);
        PlateUpdateHandler.FreeEmptyStringPointer();
    }
}