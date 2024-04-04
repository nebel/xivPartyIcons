using System;
using System.Linq;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI;
using PartyIcons.Api;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.Utils;
using PartyIcons.View;

namespace PartyIcons.Runtime;

public sealed class NameplateUpdater : IDisposable
{
    private readonly Settings _configuration;
    private readonly NameplateView _view;
    private readonly ViewModeSetter _modeSetter;

    [Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 5C 24 ?? 45 38 BE", DetourName = nameof(SetNamePlateDetour))]
    private readonly Hook<SetNamePlateDelegate> _setNamePlateHook = null!;

    public int DebugIcon { get; set; } = -1;

    public NameplateUpdater(Settings configuration, NameplateView view, ViewModeSetter modeSetter)
    {
        _configuration = configuration;
        _view = view;
        _modeSetter = modeSetter;

        Service.GameInteropProvider.InitializeFromAttributes(this);
    }

    public void Enable()
    {
        _setNamePlateHook.Enable();
    }

    public void Dispose()
    {
        _setNamePlateHook.Disable();
        _setNamePlateHook.Dispose();
    }

    private delegate IntPtr SetNamePlateDelegate(IntPtr addon, bool isPrefixTitle, bool displayTitle, IntPtr title,
        IntPtr name, IntPtr fcName, IntPtr prefix, uint iconID);

    public IntPtr SetNamePlateDetour(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle,
        IntPtr title, IntPtr name, IntPtr fcName, IntPtr prefix, uint iconID)
    {
        var hookResult = IntPtr.MinValue;
        try {
            SetNamePlate(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, prefix, iconID,
                ref hookResult);
        }
        catch (Exception ex) {
            Service.Log.Error(ex, "SetNamePlateDetour encountered a critical error");
        }

        return hookResult == IntPtr.MinValue
            ? _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName, prefix,
                iconID)
            : hookResult;
    }

    private unsafe void SetNamePlate(IntPtr namePlateObjectPtr, bool isPrefixTitle, bool displayTitle, IntPtr title,
        IntPtr name, IntPtr fcName, IntPtr prefix, uint iconID, ref IntPtr hookResult)
    {
        // var prefixByte = ((byte*)prefix)[0];
        // var prefixIcon = BitmapFontIcon.None;
        // if (prefixByte != 0) {
        //     prefixIcon = ((IconPayload)MemoryHelper.ReadSeStringNullTerminated(prefix).Payloads[1]).Icon;
        // }
        //
        // PluginLog.Warning(
        //     $"SetNamePlate @ 0x{namePlateObjectPtr:X}\nTitle: isPrefix=[{isPrefixTitle}] displayTitle=[{displayTitle}] title=[{SeStringUtils.PrintRawStringArg(title)}]\n" +
        //     $"name=[{SeStringUtils.PrintRawStringArg(name)}] fcName=[{SeStringUtils.PrintRawStringArg(fcName)}] prefix=[{SeStringUtils.PrintRawStringArg(prefix)}] iconID=[{iconID}]\n" +
        //     $"prefixByte=[0x{prefixByte:X}] prefixIcon=[{prefixIcon}({(int)prefixIcon})]");

        var npObject = new NamePlateObjectWrapper((AddonNamePlate.NamePlateObject*)namePlateObjectPtr);

        if (Service.ClientState.IsPvP
            || npObject is not { IsPlayer: true, NamePlateInfo: { ObjectID: not 0xE0000000 } npInfo }
            || npInfo.GetJobID() is < 1 or > JobConstants.MaxJob) {
            _view.SetupDefault(npObject);
            return;
        }

        var originalTitle = title;
        var originalName = name;
        var originalFcName = fcName;

        bool usedTextIcon;
        try {
            _view.NameplateDataForPC(npObject, ref isPrefixTitle, ref displayTitle, ref title, ref name, ref fcName,
                ref iconID, out usedTextIcon);
        }
        finally {
            if (originalName != name)
                SeStringUtils.FreePtr(name);
            if (originalTitle != title)
                SeStringUtils.FreePtr(title);
            if (originalFcName != fcName)
                SeStringUtils.FreePtr(fcName);
        }

        var status = npInfo.GetOnlineStatus();
        var isPriorityIcon = IsPriorityStatus(status);
        if (isPriorityIcon && !usedTextIcon) {
            iconID = StatusUtils.OnlineStatusToIconId(status);
        }

        hookResult = _setNamePlateHook.Original(namePlateObjectPtr, isPrefixTitle, displayTitle, title, name, fcName,
            SeStringUtils.emptyPtr, iconID);

        _view.SetupForPC(npObject, isPriorityIcon);
    }

    private bool IsPriorityStatus(Status status)
    {
        if (_configuration.UsePriorityIcons == false && status != Status.Disconnected)
            return false;

        if (_modeSetter.ZoneType == ZoneType.Foray)
            return StatusUtils.PriorityStatusesInForay.Contains(status);

        if (_modeSetter.InDuty)
            return StatusUtils.PriorityStatusesInDuty.Contains(status);

        return status != Status.None;
    }
}