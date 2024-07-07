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

public unsafe class PlateUpdateHandler
{
    private readonly PlateUpdateContext context;
    private readonly int numberIndex;
    private readonly int stringIndex;

    private ulong? gameObjectId;
    private IGameObject? gameObject;

    internal PlateUpdateHandler(PlateUpdateContext context, int arrayIndex)
    {
        this.context = context;
        numberIndex = 6 + arrayIndex * 20;
        stringIndex = arrayIndex;
    }

    internal void ResetState()
    {
        gameObjectId = null;
        gameObject = null;
        IsUpdating = (UpdateFlags & 1) != 0;
    }

    public int NamePlateKind => context.numberData->IntArray[numberIndex + (int)NamePlateGui.NamePlateNumberField.NamePlateKind];
    public int UpdateFlags => context.numberData->IntArray[numberIndex + (int)NamePlateGui.NamePlateNumberField.UpdateFlags];
    public int DrawFlags => context.numberData->IntArray[numberIndex + (int)NamePlateGui.NamePlateNumberField.DrawFlags];
    public int NamePlateIndex => context.numberData->IntArray[numberIndex + (int)NamePlateGui.NamePlateNumberField.NamePlateIndex];

    private RaptureAtkModule.NamePlateInfo* NamePlateInfo => context.raptureAtkModule->NamePlateInfoEntries.GetPointer(NamePlateIndex);


    public nint NamePlateInfoAddress => (nint)NamePlateInfo;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetNumberValue(NamePlateGui.NamePlateNumberField field)
    {
        return context.numberData->IntArray[numberIndex + (int)field];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNumberValue(NamePlateGui.NamePlateNumberField field, int value)
    {
        context.numberData->IntArray[numberIndex + (int)field] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* GetStringValueAsPointer(NamePlateGui.NamePlateStringField field)
    {
        return context.stringData->StringArray[stringIndex + (int)field];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetStringValueAsSpan(NamePlateGui.NamePlateStringField field)
    {
        return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(GetStringValueAsPointer(field));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetStringValueAsString(NamePlateGui.NamePlateStringField field)
    {
        return Encoding.UTF8.GetString(GetStringValueAsSpan(field));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SeString GetStringValueAsSeString(NamePlateGui.NamePlateStringField field)
    {
        return SeString.Parse(GetStringValueAsSpan(field));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStringValue(NamePlateGui.NamePlateStringField field, string value)
    {
        context.stringData->SetValue(stringIndex + (int)field, value, true, true, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStringValue(NamePlateGui.NamePlateStringField field, SeString value)
    {
        context.stringData->SetValue(stringIndex + (int)field, value.Encode(), true, true, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStringValue(NamePlateGui.NamePlateStringField field, ReadOnlySpan<byte> value)
    {
        context.stringData->SetValue(stringIndex + (int)field, value, true, true, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStringValue(NamePlateGui.NamePlateStringField field, byte* value)
    {
        context.stringData->SetValue(stringIndex + (int)field, value, true, true, true);
    }

    public void DebugLog()
    {
        for (var i = NamePlateGui.NamePlateStringField.Name; i <= (NamePlateGui.NamePlateStringField)250; i += 50) {
            Service.Log.Debug($"    S[{i}] {GetStringValueAsString(i)}");
        }
        for (var i = NamePlateGui.NamePlateNumberField.NamePlateKind; i < (NamePlateGui.NamePlateNumberField)20; i++) {
            Service.Log.Debug($"    N[{i}] {GetNumberValue(i)}");
        }
    }
}