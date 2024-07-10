using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyIcons.Dalamud;

/// <summary>
/// Contains information related to the pending nameplate data update. This is only valid for a single frame and should
/// not be kept across frames.
/// </summary>
public unsafe class NamePlateUpdateContext
{
    internal readonly RaptureAtkModule* raptureAtkModule;
    internal AddonNamePlate* addon;
    internal NumberArrayData* numberData;
    internal StringArrayData* stringData;
    internal bool hasBuilders;

    internal AddonNamePlate.NamePlateIntArrayData* numberStruct;

    public nint RAM => (nint)raptureAtkModule;

    /// <summary>
    /// The number of active nameplates. The actual number visible may be lower than this in cases where some nameplates
    /// are hidden by default (based on in-game "Display Name Settings" and so on).
    /// </summary>
    public int ActiveNamePlateCount { get; private set; }

    /// <summary>
    /// Returns true if the game is currently performing a full update of all active nameplates.
    /// </summary>
    public bool IsFullUpdate { get; private set; }

    /// <summary>
    /// Returns the address of the NamePlate addon.
    /// </summary>
    public nint AddonAddress => (nint)addon;

    /// <summary>
    /// Returns the address of the NamePlate addon's number array data container.
    /// </summary>
    public nint NumberArrayDataAddress => (nint)numberData;

    /// <summary>
    /// Returns the address of the NamePlate addon's string array data container.
    /// </summary>
    public nint StringArrayDataAddress => (nint)stringData;

    /// <summary>
    /// Returns the address of the first entry in the NamePlate addon's int array.
    /// </summary>
    public nint NumberArrayDataEntryAddress => (nint)numberStruct;

    internal NamePlateUpdateContext(AddonRequestedUpdateArgs args)
    {
        raptureAtkModule = RaptureAtkModule.Instance();
        ResetState(args);
    }

    internal void ResetState(AddonRequestedUpdateArgs args)
    {
        addon = (AddonNamePlate*)args.Addon;
        numberData = ((NumberArrayData**)args.NumberArrayData)[NamePlateGui.NumberArrayIndex];
        numberStruct = (AddonNamePlate.NamePlateIntArrayData*)numberData->IntArray;
        stringData = ((StringArrayData**)args.StringArrayData)[NamePlateGui.StringArrayIndex];
        hasBuilders = false;

        ActiveNamePlateCount = numberStruct->ActiveNamePlateCount;
        IsFullUpdate = addon->DoFullUpdate != 0;
    }
}