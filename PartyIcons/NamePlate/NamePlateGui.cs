using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;

namespace PartyIcons.Dalamud;

public sealed class NamePlateGui : IDisposable
{
    public const int NumberArrayIndex = 5;
    public const int StringArrayIndex = 4;
    public const int NumberArrayFullUpdateIndex = 4;

    private NamePlateUpdateContext? updateContext;
    private NamePlateUpdateHandler[] updateHandlers = [];

    public event OnPlateUpdateDelegate? OnChangedPlatesPreUpdate;
    public event OnPlateUpdateDelegate? OnAllPlatesPreUpdate;

    public delegate void OnPlateUpdateDelegate(NamePlateUpdateContext context, ReadOnlySpan<NamePlateUpdateHandler> handlers);

    public void Enable()
    {
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "NamePlate", OnPreRequestedUpdate);
    }

    public void Dispose()
    {
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "NamePlate", OnPreRequestedUpdate);
        NamePlateUpdateHandler.FreeEmptyStringPointer();
    }

    public unsafe void RequestRedraw()
    {
        var addon = (AddonNamePlate*)Service.GameGui.GetAddonByName("NamePlate");
        if (addon != null) {
            addon->DoFullUpdate = 1;

            var raptureAtkModule = RaptureAtkModule.Instance();
            var namePlateNumberArrayData = raptureAtkModule->AtkArrayDataHolder.NumberArrays[5];
            namePlateNumberArrayData->SetValue(NumberArrayFullUpdateIndex, 1);
        }
    }

    private void CreateHandlers(NamePlateUpdateContext context)
    {
        var handlers = new List<NamePlateUpdateHandler>();
        for (var i = 0; i < AddonNamePlate.NumNamePlateObjects; i++) {
            handlers.Add(new NamePlateUpdateHandler(context, i));
        }
        updateHandlers = handlers.ToArray();
    }

    private void OnPreRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (OnAllPlatesPreUpdate == null && OnChangedPlatesPreUpdate == null) {
            return;
        }

        var reqArgs = (AddonRequestedUpdateArgs)args;
        if (updateContext == null) {
            updateContext = new NamePlateUpdateContext(reqArgs);
            CreateHandlers(updateContext);
        }
        else {
            updateContext.ResetState(reqArgs);
        }

        var activeHandlers = updateHandlers.AsSpan(0, updateContext.ActiveNamePlateCount);

        if (updateContext.IsFullUpdate) {
            foreach (var handler in activeHandlers) {
                handler.ResetState();
            }
            OnAllPlatesPreUpdate?.Invoke(updateContext, activeHandlers);
            OnChangedPlatesPreUpdate?.Invoke(updateContext, activeHandlers);
            if (updateContext.hasBuilders)
                ApplyBuilders(activeHandlers);
        }
        else {
            var changedHandlers = new List<NamePlateUpdateHandler>();
            foreach (var handler in activeHandlers) {
                handler.ResetState();
                if (handler.IsUpdating) {
                    changedHandlers.Add(handler);
                }
            }

            if (OnAllPlatesPreUpdate is not null) {
                OnAllPlatesPreUpdate?.Invoke(updateContext, activeHandlers);
                OnChangedPlatesPreUpdate?.Invoke(updateContext, changedHandlers.ToArray().AsSpan());
                if (updateContext.hasBuilders)
                    ApplyBuilders(activeHandlers);
            }
            else if (changedHandlers.Count != 0) {
                var changedHandlersSpan = changedHandlers.ToArray().AsSpan();
                OnChangedPlatesPreUpdate?.Invoke(updateContext, changedHandlersSpan);
                if (updateContext.hasBuilders)
                    ApplyBuilders(changedHandlersSpan);
            }
        }
    }

    private static void ApplyBuilders(Span<NamePlateUpdateHandler> handlers)
    {
        foreach (var handler in handlers) {
            if (handler.PartBuilder is { } partBuilder) {
                partBuilder.NameBuilder?.Apply(handler);
                partBuilder.FreeCompanyTagBuilder?.Apply(handler);
                partBuilder.TitleBuilder?.Apply(handler);
            }
        }
    }
}