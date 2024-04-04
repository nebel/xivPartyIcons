using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace PartyIcons.Api;

public unsafe ref struct NamePlateObjectWrapper
{
    private readonly AddonNamePlate.NamePlateObject* _pointer;
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
                var atkModule = ModuleCache.RaptureAtkModulePtr;
                if (atkModule == null) {
                    Service.Log.Verbose("[NamePlateObjectWrapper] RaptureAtkModule was null");
                    throw new Exception("Cannot get NamePlateInfo as RaptureAtkModule was null");
                }

                _namePlateInfoWrapper = new NamePlateInfoWrapper(atkModule->NamePlateInfoEntriesSpan.GetPointer(Index));
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

    public override string ToString()
    {
        var p = *_pointer;
        var nameRaw = MemoryHelper.ReadSeStringNullTerminated(new IntPtr(p.NameText->GetText()));
        var name = string.Join("//", nameRaw.Payloads.Select(payload => payload.ToString()));
        var nameScale = p.NameText->AtkResNode.ScaleX;
        var iconScale = p.IconImageNode->AtkResNode.ScaleX;
        var iconPos = $"[x={p.IconXAdjust} y={p.IconYAdjust}]";

        var icon = GetImageNodeInfo(p.IconImageNode);
        var image2 = GetImageNodeInfo(p.ImageNode2);
        var image3 = GetImageNodeInfo(p.ImageNode3);
        var image4 = GetImageNodeInfo(p.ImageNode4);
        var image5 = GetImageNodeInfo(p.ImageNode5);

        var images = string.Join("\n", [icon, image2 /*, image3, image4, image5*/]);

        var objectId = NamePlateInfo.ObjectID;
        var obj = Service.ObjectTable.SearchById(objectId);
        var objName = obj != null
            ? $"{obj.Address.ToInt64():X}:{obj.ObjectId:X}[{Index}] - {obj.ObjectKind} - {obj.Name}"
            : $"???????????:{objectId:X}[{Index}] - ? - ?";

        return
            $"{objName}\n{name}\nisVisible={p.IsVisible} isPlayer={p.IsPlayerCharacter} onlineStatus={NamePlateInfo.GetOnlineStatusName()}\nnameScale={nameScale} iconScale={iconScale} iconPos={iconPos}\n{images}";
    }

    private string GetImageNodeInfo(AtkImageNode* iNode)
    {
        var sb = new StringBuilder();
        sb.Append($"wrap: {iNode->WrapMode}, flags: {iNode->Flags} / ");
        if (iNode->PartsList != null) {
            if (iNode->PartId > iNode->PartsList->PartCount) {
                sb.Append($"part id({iNode->PartId}) > part count({iNode->PartsList->PartCount})? / ");
            }
            else {
                var part = iNode->PartsList->Parts[iNode->PartId];
                var textureInfo = part.UldAsset;
                var texType = textureInfo->AtkTexture.TextureType;
                sb.Append(
                    $"texture type: {texType} part_id={iNode->PartId} part_id_count={iNode->PartsList->PartCount} / ");
                if (texType == TextureType.Resource) {
                    var texFileNamePtr = textureInfo->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle
                        .FileName;
                    var texString = Marshal.PtrToStringAnsi(new IntPtr(texFileNamePtr.BufferPtr));
                    sb.Append($"texture path: {texString}");
                }
                else if (texType == TextureType.KernelTexture) {
                    sb.Append("KernelTexture");
                }
            }
        }
        else {
            sb.Append("no texture loaded");
        }

        return sb.ToString();
    }
}