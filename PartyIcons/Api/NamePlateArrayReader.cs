using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace PartyIcons.Api;

public unsafe class NamePlateArrayReader : IEnumerable<NamePlateObjectWrapper>
{
    private readonly AddonNamePlate.NamePlateObject* _pointer = GetObjectArrayPointer();
    private const int MaxNameplates = 50; // FIXME: read from AddonNamePlate.NumNamePlateObjects

    private static AddonNamePlate.NamePlateObject* GetObjectArrayPointer()
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
        var baseAddr = ((nint)GetObjectArrayPointer()).ToInt64();
        var targetAddr = ((nint)namePlateObjectPtr).ToInt64();
        var npObjectSize = Marshal.SizeOf(typeof(AddonNamePlate.NamePlateObject));
        var index = (int)((targetAddr - baseAddr) / npObjectSize);
        if (index is < 0 or >= MaxNameplates) {
            Service.Log.Verbose("NamePlateObject index was out of bounds");
            return -1;
        }

        return index;
    }

    private bool IsValidPointer()
    {
        return _pointer != null;
    }

    private NamePlateObjectWrapper GetUnchecked(int index)
    {
        var ptr = &_pointer[index];
        return new NamePlateObjectWrapper(ptr, index);
    }

    public IEnumerator<NamePlateObjectWrapper> GetEnumerator()
    {
        if (IsValidPointer()) {
            for (var i = 0; i < MaxNameplates; i++) {
                yield return GetUnchecked(i);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}