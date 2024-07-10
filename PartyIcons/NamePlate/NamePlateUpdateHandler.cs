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

public unsafe class NamePlateUpdateHandler : ICloneable
{
    private readonly NamePlateUpdateContext context;
    private readonly int arrayIndex;

    private ulong? gameObjectId;
    private IGameObject? gameObject;
    private NamePlateBaseInfo? baseInfo;
    private NamePlatePartBuilder? namePlatePartBuilder;

    internal static readonly byte* EmptyStringPointer = CreateEmptyStringPointer();

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

    internal NamePlateUpdateHandler(NamePlateUpdateContext context, int arrayIndex)
    {
        this.context = context;
        this.arrayIndex = arrayIndex;
    }

    internal void ResetState()
    {
        gameObjectId = null;
        gameObject = null;
        baseInfo = null;
        namePlatePartBuilder = null;
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

    #region Number Data

    private AddonNamePlate.NamePlateIntArrayData.NamePlateObjectIntArrayData* ObjectData => context.numberStruct->ObjectData.GetPointer(arrayIndex);

    public NamePlateKind NamePlateKind => (NamePlateKind)ObjectData->NamePlateKind;

    public int UpdateFlags
    {
        get => ObjectData->UpdateFlags;
        private set => ObjectData->UpdateFlags = value;
    }

    public uint TextColor
    {
        get => (uint)ObjectData->NameTextColor;
        set
        {
            if (value != TextColor) UpdateFlags |= 2;
            ObjectData->NameTextColor = unchecked((int)value);
        }
    }

    public uint EdgeColor
    {
        get => (uint)ObjectData->NameEdgeColor;
        set
        {
            if (value != EdgeColor) UpdateFlags |= 2;
            ObjectData->NameEdgeColor = unchecked((int)value);
        }
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

    public int NamePlateIndex => ObjectData->NamePlateObjectIndex;

    public int DrawFlags
    {
        get => ObjectData->DrawFlags;
        private set => ObjectData->DrawFlags = value;
    }

    // public int VisibilityFlags
    // {
    //     get => ObjectData->VisibilityFlags;
    //     private set => ObjectData->VisibilityFlags = value;
    // }

    #endregion

    #region Number Data (Derived)

    /// <summary>
    /// Indicates whether this nameplate is undergoing a major update or not. This is usually true when a nameplate has just appeared or
    /// something meaningful about the entity has changed (e.g. its job or status). This flag is reset by the game during the update
    /// process (during requested update and before draw).
    /// </summary>
    public bool IsUpdating => (UpdateFlags & 1) != 0;

    /// <summary>
    /// If true, the title (when visible) will be displayed above the object's name instead of below.
    /// </summary>
    public bool IsPrefixTitle
    {
        get => (DrawFlags & 1) != 0;
        set => DrawFlags = value ? DrawFlags | 1 : DrawFlags & ~1;
    }

    /// <summary>
    /// Determines whether the title should be displayed at all.
    /// </summary>
    public bool DisplayTitle
    {
        get => (DrawFlags & 0x80) == 0;
        set => DrawFlags = value ? DrawFlags & ~0x80 : DrawFlags | 0x80;
    }

    #endregion

    #region String Data

    public SeString Name
    {
        get => GetStringValueAsSeString(NamePlateStringField.Name);
        set => SetField(NamePlateStringField.Name, value);
    }

    public SeString Title
    {
        get => GetStringValueAsSeString(NamePlateStringField.Title);
        set => SetField(NamePlateStringField.Title, value);
    }

    public SeString FreeCompanyTag
    {
        get => GetStringValueAsSeString(NamePlateStringField.FreeCompanyTag);
        set => SetField(NamePlateStringField.FreeCompanyTag, value);
    }

    public SeString StatusPrefix
    {
        get => GetStringValueAsSeString(NamePlateStringField.StatusPrefix);
        set => SetField(NamePlateStringField.StatusPrefix, value);
    }

    public SeString TargetSuffix
    {
        get => GetStringValueAsSeString(NamePlateStringField.TargetSuffix);
        set => SetField(NamePlateStringField.TargetSuffix, value);
    }

    public SeString LevelPrefix
    {
        get => GetStringValueAsSeString(NamePlateStringField.LevelPrefix);
        set => SetField(NamePlateStringField.LevelPrefix, value);
    }

    public void RemoveName() => RemoveField(NamePlateStringField.Name);
    public void RemoveTitle() => RemoveField(NamePlateStringField.Title);
    public void RemoveFreeCompanyTag() => RemoveField(NamePlateStringField.FreeCompanyTag);
    public void RemoveStatusPrefix() => RemoveField(NamePlateStringField.StatusPrefix);
    public void RemoveTargetSuffix() => RemoveField(NamePlateStringField.TargetSuffix);
    public void RemoveLevelPrefix() => RemoveField(NamePlateStringField.LevelPrefix);

    #endregion

    #region String Data Builders

    internal NamePlatePartBuilder PartBuilder => namePlatePartBuilder ??= new NamePlatePartBuilder(context);

    public NamePlateSimplePartBuilder NameBuilder => PartBuilder.Name;
    public NamePlateQuotedPartBuilder TitleBuilder => PartBuilder.Title;
    public NamePlateQuotedPartBuilder FreeCompanyTagBuilder => PartBuilder.FreeCompanyTag;

    #endregion

    #region String Data Accessors

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* GetStringValueAsPointer(NamePlateStringField field)
    {
        return context.stringData->StringArray[arrayIndex + (int)field];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetStringValueAsSpan(NamePlateStringField field)
    {
        return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(GetStringValueAsPointer(field));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetStringValueAsString(NamePlateStringField field)
    {
        return Encoding.UTF8.GetString(GetStringValueAsSpan(field));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SeString GetStringValueAsSeString(NamePlateStringField field)
    {
        return SeString.Parse(GetStringValueAsSpan(field));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetField(NamePlateStringField field, string value)
    {
        context.stringData->SetValue(arrayIndex + (int)field, value, true, true, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetField(NamePlateStringField field, SeString value)
    {
        context.stringData->SetValue(arrayIndex + (int)field, value.Encode(), true, true, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetField(NamePlateStringField field, ReadOnlySpan<byte> value)
    {
        context.stringData->SetValue(arrayIndex + (int)field, value, true, true, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetField(NamePlateStringField field, byte* value)
    {
        context.stringData->SetValue(arrayIndex + (int)field, value, true, true, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveField(NamePlateStringField field)
    {
        context.stringData->SetValue(arrayIndex + (int)field, EmptyStringPointer, true, false, true);
    }

    #endregion

    public object Clone()
    {
        return MemberwiseClone();
    }
}