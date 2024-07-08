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
    public NamePlateGui()
    {
        Service.Log.Error("STARTING NamePlateGui");
    }

    public void Enable()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "NamePlate", OnPreRequestedUpdate);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "NamePlate", OnPreDraw);
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

    public event OnPlateUpdateDelegate? OnChangedPlatesPreDraw;

    public event OnPlateUpdateDelegate? OnAllPlatesPreDraw;

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

        // unsafe {
        //     var numStruct = (AddonNamePlate.NamePlateIntArrayData*)updateContext.numberData->IntArray;
        //     Service.Log.Info($"{Framework.Instance()->FrameCounter} addonNamePlate->DoFullUpdate={updateContext.addon->DoFullUpdate} intArray->DoFullUpdate={numStruct->DoFullUpdate}");
        // }

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
        changedUpdateHandlers = updatingPlates.ToArray();

        OnAllPlatesPreUpdate?.Invoke(updateContext, updateHandlers.AsSpan(0, numPlatesShown));

        if (updateContext.IsFullUpdate) {
            OnChangedPlatesPreUpdate?.Invoke(updateContext, updateHandlers.AsSpan(0, numPlatesShown));
        }
        else if (changedUpdateHandlers.Length != 0) {
            OnChangedPlatesPreUpdate?.Invoke(updateContext, changedUpdateHandlers.AsSpan());
        }
    }

    private void OnPreDraw(AddonEvent type, AddonArgs args)
    {
        if (updateContext == null) return;

        OnAllPlatesPreDraw?.Invoke(updateContext, updateHandlers.AsSpan(0, updateContext.ActiveNamePlateCount));

        if (updateContext.IsFullUpdate) {
            OnChangedPlatesPreDraw?.Invoke(updateContext, updateHandlers.AsSpan(0, updateContext.ActiveNamePlateCount));
        }
        else if (changedUpdateHandlers.Length != 0) {
            OnChangedPlatesPreDraw?.Invoke(updateContext, changedUpdateHandlers.AsSpan());
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
                handler.SetNumberValue(NamePlateNumberField.MarkerIconId, 66181 + (int)handler.NamePlateKind);
            }
            else {
                handler.SetNumberValue(NamePlateNumberField.MarkerIconId, 66161 + (int)handler.NamePlateKind);
            }
            // handler.SetNumberValue(NamePlateNumberField.NameTextColor, unchecked((int)0xFF00FF00));
            // handler.SetNumberValue(NamePlateNumberField.NameEdgeColor, unchecked((int)0xFFFF0000));
        }
    }

    public void Dispose()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "NamePlate", OnPreRequestedUpdate);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreDraw, "NamePlate", OnPreDraw);
        PlateUpdateHandler.FreeEmptyStringPointer();
    }
}