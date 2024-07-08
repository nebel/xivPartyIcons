using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyIcons.Dalamud;

public unsafe class PlateUpdateContext
{
    internal readonly RaptureAtkModule* raptureAtkModule;
    internal AddonNamePlate* addon;
    internal NumberArrayData* numberData;
    internal StringArrayData* stringData;

    internal AddonNamePlate.NamePlateIntArrayData* numberStruct;

    internal PlateUpdateContext(AddonRequestedUpdateArgs args)
    {
        raptureAtkModule = RaptureAtkModule.Instance();
        ResetState(args);
    }

    internal void ResetState(AddonRequestedUpdateArgs args)
    {
        addon = (AddonNamePlate*)args.Addon;
        numberData = ((NumberArrayData**)args.NumberArrayData)[5];
        numberStruct = (AddonNamePlate.NamePlateIntArrayData*)numberData->IntArray;
        stringData = ((StringArrayData**)args.StringArrayData)[4];

        ActiveNamePlateCount = numberStruct->ActiveNamePlateCount;
        IsFullUpdate = addon->DoFullUpdate != 0;
    }

    public int ActiveNamePlateCount { get; private set; }

    public bool IsFullUpdate { get; private set; }
}