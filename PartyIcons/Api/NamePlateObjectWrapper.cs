using System;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace PartyIcons.Api;

public unsafe struct NamePlateObjectWrapper
{
    internal readonly AddonNamePlate.NamePlateObject* _pointer;

    private int _index;
    private NamePlateInfoWrapper _namePlateInfoWrapper;

    public NamePlateObjectWrapper(AddonNamePlate.NamePlateObject* pointer, int index = -1)
    {
        _pointer = pointer;
        _index = index;
    }

    private int Index
    {
        get
        {
            if (_index == -1) {
                _index = NamePlateArrayReader.GetIndexOf(_pointer);
            }

            return _index;
        }
    }

    public NamePlateInfoWrapper NamePlateInfo
    {
        get
        {
            if (_namePlateInfoWrapper.ObjectID == default) {
                var atkModule = XivApi.RaptureAtkModulePtr;
                if (atkModule == null) {
                    Service.Log.Verbose($"[{GetType().Name}] RaptureAtkModule was null");
                    throw new Exception("Cannot get NamePlateInfo as RaptureAtkModule was null");
                }

                var infoArray = &atkModule->NamePlateInfoArray;
                _namePlateInfoWrapper = new NamePlateInfoWrapper(&infoArray[Index]);
            }

            return _namePlateInfoWrapper;
        }
    }

    public bool IsVisible => _pointer->IsVisible;

    public bool IsPlayer => _pointer->IsPlayerCharacter;

    /// <returns>True if the icon scale was changed.</returns>
    public bool SetIconScale(float scale, bool force = false)
    {
        if (force || !IsIconScaleEqual(scale)) {
            _pointer->IconImageNode->AtkResNode.SetScale(scale, scale);
            return true;
        }

        return false;
    }

    /// <returns>True if the name scale was changed.</returns>
    public bool SetNameScale(float scale, bool force = false)
    {
        if (force || !IsNameScaleEqual(scale)) {
            _pointer->NameText->AtkResNode.SetScale(scale, scale);
            return true;
        }

        return false;
    }

    public void SetIconPosition(short x, short y)
    {
        _pointer->IconXAdjust = x;
        _pointer->IconYAdjust = y;
    }

    private static bool NearlyEqual(float left, float right, float tolerance)
    {
        return Math.Abs(left - right) <= tolerance;
    }

    private bool IsIconScaleEqual(float scale)
    {
        var node = _pointer->IconImageNode->AtkResNode;
        return
            NearlyEqual(scale, node.ScaleX, ScaleTolerance) &&
            NearlyEqual(scale, node.ScaleY, ScaleTolerance);
    }

    private bool IsNameScaleEqual(float scale)
    {
        var node = _pointer->NameText->AtkResNode;
        return
            NearlyEqual(scale, node.ScaleX, ScaleTolerance) &&
            NearlyEqual(scale, node.ScaleY, ScaleTolerance);
    }

    private const float ScaleTolerance = 0.001f;
}