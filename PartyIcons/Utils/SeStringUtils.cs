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
    public static readonly IntPtr EmptyPtr = SeStringToPtr(Text(""));
    public static readonly IntPtr FullwidthSpacePtr = SeStringToPtr(Text("　"));

    public static void Initialize()
    {
        // EmptyPtr = SeStringToPtr(Text(""));
        // FullwidthSpacePtr = SeStringToPtr(Text("　"));
    }

    public static void Dispose() { }

    public static SeString SeStringFromPtr(IntPtr seStringPtr)
    {
        byte b;
        var offset = 0;

        unsafe
        {
            while ((b = *(byte*) (seStringPtr + offset)) != 0)
            {
                offset++;
            }
        }

        var bytes = new byte[offset];
        Marshal.Copy(seStringPtr, bytes, 0, offset);

        return SeString.Parse(bytes);
    }

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
        seString.Append(new UIForegroundPayload(color));
        seString.Append(new UIGlowPayload(51)); // Black glow
        seString.Append(new TextPayload(text));
        seString.Append(UIGlowPayload.UIGlowOff);
        seString.Append(UIForegroundPayload.UIForegroundOff);

        return seString;
    }

    public static SeString Icon(BitmapFontIcon icon, string? prefix = null)
    {
        var seString = new SeString(new List<Payload>());

        if (prefix != null)
        {
            seString.Append(new TextPayload(prefix));
        }

        seString.Append(new IconPayload(icon));

        return seString;
    }

    public static string PrintRawStringArg(IntPtr arg)
    {
        var seString = MemoryHelper.ReadSeStringNullTerminated(arg);
        return string.Join("", seString.Payloads.Select(payload => $"[{payload}]"));
    }
}
