using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace PartyIcons.Api;

public unsafe class NamePlateArrayReader
{
    public static readonly int NumNameplates = AddonNamePlate.NumNamePlateObjects;
    private readonly AddonNamePlate.NamePlateObject* _pointer = GetNamePlateObjectArrayPointer();

    private static AddonNamePlate.NamePlateObject* GetNamePlateObjectArrayPointer()
    {
        var addonPtr = (AddonNamePlate*)Service.GameGui.GetAddonByName("NamePlate");
        if (addonPtr == null) {
            return null;
        }

        var objectArrayPtr = addonPtr->NamePlateObjectArray;
        if (objectArrayPtr == null) {
            Service.Log.Verbose("NamePlateObjectArray was null");
        }

        return objectArrayPtr;
    }

    public static int GetIndexOf(AddonNamePlate.NamePlateObject* namePlateObjectPtr)
    {
        var baseAddr = ((nint)GetNamePlateObjectArrayPointer()).ToInt64();
        var targetAddr = ((nint)namePlateObjectPtr).ToInt64();
        var npObjectSize = Marshal.SizeOf(typeof(AddonNamePlate.NamePlateObject));
        var index = (int)((targetAddr - baseAddr) / npObjectSize);
        if (index < 0 || index >= NumNameplates) {
            Service.Log.Verbose("NamePlateObject index was out of bounds");
            return -1;
        }

        return index;
    }

    internal bool HasValidPointer()
    {
        return _pointer != null;
    }

    internal NamePlateObjectWrapper GetUnchecked(int index)
    {
        var ptr = &_pointer[index];
        return new NamePlateObjectWrapper(ptr, index);
    }
}