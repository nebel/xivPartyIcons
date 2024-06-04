using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;

namespace PartyIcons.Utils;

public static class SeStringUtils
{
    public static IntPtr EmptyPtr;

    public static void Initialize()
    {
        EmptyPtr = SeStringToPtr(Text(""));
    }

    public static void Dispose() { }

    public static IntPtr SeStringToPtr(SeString seString)
    {
        var bytes = seString.Encode();
        var pointer = Marshal.AllocHGlobal(bytes.Length + 1);
        Marshal.Copy(bytes, 0, pointer, bytes.Length);
        Marshal.WriteByte(pointer, bytes.Length, 0);

        return pointer;
    }

    public static void FreePtr(IntPtr seStringPtr)
    {
        if (seStringPtr != EmptyPtr)
        {
            Marshal.FreeHGlobal(seStringPtr);
        }
    }

    public static SeString Text(string rawText)
    {
        var seString = new SeString(new List<Payload>());
        seString.Append(new TextPayload(rawText));

        return seString;
    }

    public static SeString Text(string text, ushort color)
    {
        var seString = new SeString(new List<Payload>());
        seString.Append(new UIGlowPayload(51)); // Black glow
        seString.Append(new UIForegroundPayload(color));
        seString.Append(new TextPayload(text));
        seString.Append(UIForegroundPayload.UIForegroundOff);
        seString.Append(UIGlowPayload.UIGlowOff);

        return seString;
    }

    public static string PrintRawStringArg(IntPtr arg)
    {
        var seString = MemoryHelper.ReadSeStringNullTerminated(arg);
        return string.Join("", seString.Payloads.Select(payload => $"[{payload}]"));
    }
}
