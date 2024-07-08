using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.Interop;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PartyIcons.Dalamud;

public unsafe class PlateUpdateHandler : ICloneable
{
    private readonly PlateUpdateContext context;
    private readonly int arrayIndex;
    private readonly int numberIndex;
    private readonly int stringIndex;

    private ulong? gameObjectId;
    private IGameObject? gameObject;
    private NamePlateBaseInfo? baseInfo;

    private static readonly byte* EmptyStringPointer = CreateEmptyStringPointer();

    private static byte* CreateEmptyStringPointer()
    {
        var pointer = Marshal.AllocHGlobal(1);
        Marshal.WriteByte(pointer, 0, 0);
        return (byte*)pointer;
    }

    internal static void FreeEmptyStringPointer()
    {
        Marshal.FreeHGlobal((nint)EmptyStringPointer);
    }

    internal PlateUpdateHandler(PlateUpdateContext context, int arrayIndex)
    {
        this.context = context;
        this.arrayIndex = arrayIndex;
        numberIndex = 6 + arrayIndex * 20;
        stringIndex = arrayIndex;
    }

    internal void ResetState()
    {
        gameObjectId = null;
        gameObject = null;
        baseInfo = null;
        IsUpdating = (UpdateFlags & 1) != 0;
    }

    private AddonNamePlate.NamePlateIntArrayData.NamePlateObjectIntArrayData* ObjectData => context.numberStruct->ObjectData.GetPointer(arrayIndex);

    public NamePlateKind NamePlateKind => (NamePlateKind)ObjectData->NamePlateKind;

    public int UpdateFlags
    {
        get => ObjectData->UpdateFlags;
        private set => ObjectData->UpdateFlags = value;
    }

    public int MarkerIconId
    {
        get => ObjectData->MarkerIconId;
        set => ObjectData->MarkerIconId = value;
    }

    public int NameIconId
    {
        get => ObjectData->NameIconId;
        set => ObjectData->NameIconId = value;
    }

    public uint TextColor
    {
        get => (uint)ObjectData->NameTextColor;
        set
        {
            UpdateFlags |= 2;
            ObjectData->NameTextColor = unchecked((int)value);
        }
    }

    public uint EdgeColor
    {
        get => (uint)ObjectData->NameEdgeColor;
        set
        {
            UpdateFlags |= 2;
            ObjectData->NameEdgeColor = unchecked((int)value);
        }
    }

    public int NamePlateIndex => ObjectData->NamePlateObjectIndex;

    public int DrawFlags
    {
        get => ObjectData->DrawFlags;
        private set => ObjectData->DrawFlags = value;
    }

    public bool IsPrefixTitle
    {
        get => (DrawFlags & 1) != 0;
        set => DrawFlags = value ? DrawFlags | 1 : DrawFlags & ~1;
    }

    public bool DisplayTitle
    {
        get => (DrawFlags & 0x80) == 0;
        set => DrawFlags = value ? DrawFlags & ~0x80 : DrawFlags | 0x80;
    }

    private RaptureAtkModule.NamePlateInfo* NamePlateInfo => context.raptureAtkModule->NamePlateInfoEntries.GetPointer(NamePlateIndex);

    public nint NamePlateInfoAddress => (nint)NamePlateInfo;

    public NamePlateBaseInfo BaseInfo => baseInfo ??= new NamePlateBaseInfo(NamePlateInfo);

    private AddonNamePlate.NamePlateObject* NamePlateObject => &context.addon->NamePlateObjectArray[NamePlateIndex];

    public nint NamePlateObjectAddress => (nint)NamePlateObject;

    public ulong GameObjectId => gameObjectId ??= NamePlateInfo->ObjectId;
    public IGameObject? GameObject => gameObject ??= Service.ObjectTable.SearchById(GameObjectId);
    public IBattleChara? BattleChara => GameObject as IBattleChara;
    public IPlayerCharacter? PlayerCharacter => GameObject as IPlayerCharacter;

    /// <summary>
    /// Returns whether this nameplate is undergoing a major update or not. This is usually true when a nameplate has just appeared or
    /// something meaningful about the entity has changed (e.g. its job or status). This value is reset by the game during the update
    /// process, but we cache its value here so that it stays the same for both update and draw events.
    /// </summary>
    public bool IsUpdating { get; private set; }

    #region Array Data Accessors

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetNumberValue(NamePlateNumberField field)
    {
        return context.numberData->IntArray[numberIndex + (int)field];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetNumberValue(NamePlateNumberField field, int value)
    {
        context.numberData->IntArray[numberIndex + (int)field] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte* GetStringValueAsPointer(NamePlateStringField field)
    {
        return context.stringData->StringArray[stringIndex + (int)field];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<byte> GetStringValueAsSpan(NamePlateStringField field)
    {
        return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(GetStringValueAsPointer(field));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string GetStringValueAsString(NamePlateStringField field)
    {
        return Encoding.UTF8.GetString(GetStringValueAsSpan(field));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SeString GetStringValueAsSeString(NamePlateStringField field)
    {
        return SeString.Parse(GetStringValueAsSpan(field));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetField(NamePlateStringField field, string value)
    {
        context.stringData->SetValue(stringIndex + (int)field, value, true, true, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetField(NamePlateStringField field, SeString value)
    {
        context.stringData->SetValue(stringIndex + (int)field, value.Encode(), true, true, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetField(NamePlateStringField field, ReadOnlySpan<byte> value)
    {
        context.stringData->SetValue(stringIndex + (int)field, value, true, true, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetField(NamePlateStringField field, byte* value)
    {
        context.stringData->SetValue(stringIndex + (int)field, value, true, true, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ClearField(NamePlateStringField field)
    {
        context.stringData->SetValue(stringIndex + (int)field, EmptyStringPointer, true, false, true);
    }

    #endregion

    public void DebugLog()
    {
        Service.Log.Debug($"  {BaseInfo.Name}");
        for (var i = 0; i <= 250; i += 50) {
            Service.Log.Debug($"    {BaseInfo.Name} S[{i}] {GetStringValueAsString((NamePlateStringField)i)}");
        }
        for (var i = 0; i < 20; i++) {
            Service.Log.Debug($"    {BaseInfo.Name} N[{i}] {GetNumberValue((NamePlateNumberField)i)}");
        }
        Service.Log.Debug($"    {BaseInfo.IsDirty}");
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}