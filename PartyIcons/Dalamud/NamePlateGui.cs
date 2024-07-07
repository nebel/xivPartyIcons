using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;

namespace PartyIcons.Dalamud;

public unsafe class NamePlateGui : IDisposable
{
    [Signature("40 56 57 41 56 41 57 48 81 EC ?? ?? ?? ?? 48 8B 84 24 ?? ?? ?? ??", DetourName = nameof(UpdateNameplateDetour))]
    readonly Hook<UpdateNameplateDelegate>? nameplateHook = null;

    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 4C 89 44 24 ?? 57 41 54 41 55 41 56 41 57 48 83 EC 20 48 8B 74 24 ??", DetourName = nameof(UpdateNameplateNpcDetour))]
    readonly Hook<UpdateNameplateNpcDelegate>? nameplateHookMinion = null;

    [Signature("4C 8B DC 41 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 49 8B 40 20", DetourName = nameof(SetPlayerNamePlateParentDetour))]
    readonly Hook<SetPlayerNamePlateParentDelegate>? setPlayerNamePlateParentHook = null;

    public delegate void* UpdateNameplateDelegate(RaptureAtkModule* raptureAtkModule, RaptureAtkModule.NamePlateInfo* namePlateInfo, NumberArrayData* numArray, StringArrayData* stringArray, BattleChara* battleChara, int numArrayIndex, int stringArrayIndex);

    public delegate void* UpdateNameplateNpcDelegate(RaptureAtkModule* raptureAtkModule, RaptureAtkModule.NamePlateInfo* namePlateInfo, NumberArrayData* numArray, StringArrayData* stringArray, GameObject* gameObject, int numArrayIndex, int stringArrayIndex);

    public delegate nint SetPlayerNamePlateParentDelegate(nint a1, nint a2, nint a3);

    public void* UpdateNameplateDetour(RaptureAtkModule* raptureAtkModule, RaptureAtkModule.NamePlateInfo* namePlateInfo, NumberArrayData* numArray, StringArrayData* stringArray, BattleChara* battleChara, int numArrayIndex, int stringArrayIndex)
    {
        // Service.Log.Info($"[H] UpdateBattleCharaNameplates [{namePlateInfo->Name}] numArray=0X{(nint)numArray:X} numArray=0X{(nint)stringArray:X} numArrayIndex={numArrayIndex} stringArrayIndex={stringArrayIndex}");
        // SetNameplate(namePlateInfo, (nint)battleChara);
        return nameplateHook!.Original(raptureAtkModule, namePlateInfo, numArray, stringArray, battleChara, numArrayIndex, stringArrayIndex);
    }

    public void* UpdateNameplateNpcDetour(RaptureAtkModule* raptureAtkModule, RaptureAtkModule.NamePlateInfo* namePlateInfo, NumberArrayData* numArray, StringArrayData* stringArray, GameObject* gameObject, int numArrayIndex, int stringArrayIndex)
    {
        // Service.Log.Info("[H] UpdateNpcNameplates");
        // SetNameplate(namePlateInfo, (nint)gameObject);
        return nameplateHookMinion!.Original(raptureAtkModule, namePlateInfo, numArray, stringArray, gameObject, numArrayIndex, stringArrayIndex);
    }

    public nint SetPlayerNamePlateParentDetour(nint a1, nint a2, nint a3)
    {
        // Service.Log.Error($"[H] SetPlayerNamePlateParentDetour({a1:X}, {a2:X}, {a3:X})");
        // SetNameplate(namePlateInfo, (nint)battleChara);
        return setPlayerNamePlateParentHook!.Original(a1, a2, a3);
    }

    public NamePlateGui()
    {
        Service.GameInteropProvider.InitializeFromAttributes(this);
    }

    public void Enable()
    {
        nameplateHook?.Enable();
        nameplateHookMinion?.Enable();
        setPlayerNamePlateParentHook?.Enable();
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "NamePlate", OnPreRequestedUpdate);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "NamePlate", OnPreDraw);
    }

    public enum NamePlateNumberField
    {
        NamePlateKind = 0,
        HPLabelState = 1,
        UpdateFlags = 2,
        X = 3,
        Y = 4,
        Depth = 5,
        Scale = 6,
        GaugeFillPercentage = 7,
        NameTextColor = 8,
        NameEdgeColor = 9,
        GaugeFillColor = 10,
        GaugeContainerColor = 11,
        MarkerIconId = 12,
        Icon = 13,
        UnknownAdjust14 = 14,
        NamePlateIndex = 15,
        Unknown16 = 16,
        DrawFlags = 17,
        Unknown18 = 18,
        UnknownFlags19 = 19,
    }

    public enum NamePlateStringField
    {
        Name = 0,
        Title = 50,
        FreeCompanyTag = 100,
        StatusPrefix = 150,
        TargetSuffix = 200,
        LevelPrefix = 250,
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

    private event OnPlateUpdateDelegate? OnChangedPlatesPreUpdate;

    private event OnPlateUpdateDelegate? OnAllPlatesPreUpdate;

    private event OnPlateUpdateDelegate? OnChangedPlatesPreDraw;

    private event OnPlateUpdateDelegate? OnAllPlatesPreDraw;

    public delegate void OnPlateUpdateDelegate(PlateUpdateContext context, ReadOnlySpan<PlateUpdateHandler> plates);

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

        // Service.Log.Info($"{Framework.Instance()->FrameCounter} addonNamePlate->DoFullUpdate={updateContext.IsAddonFullUpdate()} numPlates={numPlatesShown}/{numStruct->ActiveNamePlateCount}");

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
        else if (updatingPlates.Count > 1) {
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
        else if (changedUpdateHandlers.Length > 1) {
            OnChangedPlatesPreUpdate?.Invoke(updateContext, changedUpdateHandlers.AsSpan());
        }
    }

 private static readonly bool DebugUpdates = true;
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
        }
    }

    public void Dispose()
    {
        nameplateHook?.Dispose();
        nameplateHookMinion?.Dispose();
        setPlayerNamePlateParentHook?.Dispose();
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "NamePlate", OnPreRequestedUpdate);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreDraw, "NamePlate", OnPreDraw);
    }
}