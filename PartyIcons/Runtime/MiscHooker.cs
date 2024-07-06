using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PartyIcons.Runtime;

public unsafe class MiscHooker : IDisposable
{
    [Signature("40 56 57 41 56 41 57 48 81 EC ?? ?? ?? ?? 48 8B 84 24 ?? ?? ?? ??", DetourName = nameof(UpdateNameplateDetour))]
    readonly Hook<UpdateNameplateDelegate>? nameplateHook = null;

    [Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 4C 89 44 24 ?? 57 41 54 41 55 41 56 41 57 48 83 EC 20 48 8B 74 24 ??", DetourName = nameof(UpdateNameplateNpcDetour))]
    readonly Hook<UpdateNameplateNpcDelegate>? nameplateHookMinion = null;

    [Signature("4C 8B DC 41 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 49 8B 40 20", DetourName = nameof(SetPlayerNamePlateParentDetour))]
    readonly Hook<SetPlayerNamePlateParentDelegate>? setPlayerNamePlateParentHook = null;

    public delegate void* UpdateNameplateDelegate(RaptureAtkModule* raptureAtkModule, RaptureAtkModule.NamePlateInfo* namePlateInfo, NumberArrayData* numArray, StringArrayData* stringArray, BattleChara* battleChara, int numArrayIndex, int stringArrayIndex);
    public delegate void* UpdateNameplateNpcDelegate(RaptureAtkModule* raptureAtkModule, RaptureAtkModule.NamePlateInfo* namePlateInfo, NumberArrayData* numArray, StringArrayData* stringArray, GameObject* gameObject, int numArrayIndex, int stringArrayIndex);
    public delegate nint SetPlayerNamePlateParentDelegate(nint a1, nint a2, nint a3);

    public void* UpdateNameplateDetour(RaptureAtkModule* raptureAtkModule, RaptureAtkModule.NamePlateInfo* namePlateInfo, NumberArrayData* numArray, StringArrayData* stringArray, BattleChara* battleChara, int numArrayIndex, int stringArrayIndex)
    {
        // Service.Log.Info($"[H] UpdateBattleCharaNameplates [{namePlateInfo->Name}] numArray=0X{(nint)numArray:X} numArray=0X{(nint)stringArray:X} numArrayIndex={numArrayIndex} stringArrayIndex={stringArrayIndex}");
        // SetNameplate(namePlateInfo, (nint)battleChara);
        return nameplateHook!.Original(raptureAtkModule, namePlateInfo, numArray, stringArray, battleChara, numArrayIndex, stringArrayIndex);
    }

    public void* UpdateNameplateNpcDetour(RaptureAtkModule* raptureAtkModule, RaptureAtkModule.NamePlateInfo* namePlateInfo, NumberArrayData* numArray, StringArrayData* stringArray, GameObject* gameObject, int numArrayIndex, int stringArrayIndex)
    {
        // Service.Log.Info("[H] UpdateNpcNameplates");
        // SetNameplate(namePlateInfo, (nint)gameObject);
        return nameplateHookMinion!.Original(raptureAtkModule, namePlateInfo, numArray, stringArray, gameObject, numArrayIndex, stringArrayIndex);
    }

    public nint SetPlayerNamePlateParentDetour(nint a1, nint a2, nint a3)
    {
        // Service.Log.Error($"[H] SetPlayerNamePlateParentDetour({a1:X}, {a2:X}, {a3:X})");
        // SetNameplate(namePlateInfo, (nint)battleChara);
        return setPlayerNamePlateParentHook!.Original(a1, a2, a3);
    }

    public MiscHooker()
    {
        Service.GameInteropProvider.InitializeFromAttributes(this);
    }

    public void Enable()
    {
        nameplateHook?.Enable();
        nameplateHookMinion?.Enable();
        setPlayerNamePlateParentHook?.Enable();
        Service.AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "NamePlate", HandleEvent);
        Service.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "NamePlate", HandleEvent);
    }

    public enum NamePlateNumberField
    {
        NamePlateKind = 0,
        UpdateFlags = 2,
        UseOldNamePlateFont = 3,
        Icon = 13,
        NamePlateIndex = 15,
        DisplayFlags = 17,
    }

    public enum NamePlateStringField
    {
        Name = 0,
        Title = 50,
        FreeCompanyTag = 100,
        StatusPrefix = 150,
        Suffix = 200,
        LevelPrefix = 250,
    }

    public enum ObjectState
    {
        NotFound,
        Found,
        Unknown
    }

    public class PlateUpdateHandler
    {
        private readonly RaptureAtkModule* atkModule;
        private readonly AddonNamePlate* addon;
        private readonly NumberArrayData* numberData;
        private readonly StringArrayData* stringData;

        private readonly int arrayIndex;
        private readonly int numberIndex;
        private readonly int stringIndex;
        private readonly int namePlateIndex;

        private GameObjectId? gameObjectId;

        public PlateUpdateHandler(IntPtr raptureAtkModule, IntPtr addonNamePlate, IntPtr numberArrayData, IntPtr stringArrayData, int arrayIndex)
        {
            atkModule = (RaptureAtkModule*)raptureAtkModule;
            addon = (AddonNamePlate*)addonNamePlate;
            numberData = ((NumberArrayData**)numberArrayData)[5];
            stringData = ((StringArrayData**)stringArrayData)[4];

            this.arrayIndex = arrayIndex;
            numberIndex = 6 + arrayIndex * 20;
            stringIndex = arrayIndex;
            namePlateIndex = numberData->IntArray[numberIndex + (int)NamePlateNumberField.NamePlateIndex];
        }

        public int NamePlateKind => numberData->IntArray[numberIndex + (int)NamePlateNumberField.NamePlateKind];
        public int UpdateFlags => numberData->IntArray[numberIndex + (int)NamePlateNumberField.UpdateFlags];
        public int DisplayFlags => numberData->IntArray[numberIndex + (int)NamePlateNumberField.DisplayFlags];
        public int NameNamePlateIndex => namePlateIndex;

        public RaptureAtkModule.NamePlateInfo* NamePlateInfo => atkModule->NamePlateInfoEntries.GetPointer(namePlateIndex);
        public AddonNamePlate.NamePlateObject* NamePlateObject => &addon->NamePlateObjectArray[namePlateIndex];
        public GameObjectId GameObjectId => gameObjectId ??= NamePlateInfo->ObjectId;
        // public GameObjectId GameObjectId => gameObjectId ??= NamePlateInfo->ObjectId;
        public bool HasObject = false;

        public IPlayerCharacter? GetPlayer()
        {
                if (NamePlateKind != 0 || gameObjectId is not { Type: 0 } id) {
                    return null;
                }

                if (Service.ObjectTable.SearchById(id) is IPlayerCharacter c) {
                    // return player ??= c;
                    // var job = ((Character*)c.Address)->CharacterData.ClassJob;
                    // return job is < 1 or > 42 ? null : c;
                }

                return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetNumberValue(NamePlateNumberField field)
        {
            return numberData->IntArray[numberIndex + (int)field];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNumberValue(NamePlateNumberField field, int value)
        {
            Service.Log.Debug($"{arrayIndex} = {numberIndex + (int)field}");
            numberData->IntArray[numberIndex + (int)field] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte* GetStringValueAsPointer(NamePlateStringField field)
        {
            return stringData->StringArray[stringIndex + (int)field];
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
        public void SetStringValue(NamePlateStringField field, string value)
        {
            stringData->SetValue(stringIndex + (int)field, value, true, true, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStringValue(NamePlateStringField field, ReadOnlySpan<byte> value)
        {
            stringData->SetValue(stringIndex + (int)field, value, true, true, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStringValue(NamePlateStringField field, SeString value)
        {
            stringData->SetValue(stringIndex + (int)field, value.Encode(), true, true, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStringValue(NamePlateStringField field, byte* value)
        {
            stringData->SetValue(stringIndex + (int)field, value, true, true, true);
        }
    }

    private void HandleEvent(AddonEvent type, AddonArgs args)
    {
        var addonNamePlate = (AddonNamePlate*)args.Addon;
        if (addonNamePlate->DoFullUpdate != 0) {
            Service.Log.Warning("DoFullUpdate");
        }

        switch (args.Type) {
            case AddonArgsType.RequestedUpdate:
                var atkModule = RaptureAtkModule.Instance();

                var reqArgs = (AddonRequestedUpdateArgs)args;
                var numData = ((NumberArrayData**)reqArgs.NumberArrayData)[5];
                var numPlatesShown = numData->IntArray[0];

                if (type == AddonEvent.PreRequestedUpdate) {
                    for (var i = 0; i < numPlatesShown; i++) {
                        var numIndex = 6 + i * 20;
                        var namePlateKind = numData->IntArray[numIndex + (int)NamePlateNumberField.NamePlateKind];
                        var updateFlags = numData->IntArray[numIndex + (int)NamePlateNumberField.UpdateFlags];

                        var isObjectUpdated = (updateFlags & 1) != 0;
                        var isFullUpdate = addonNamePlate->DoFullUpdate != 0;
                        var isUpdating = isObjectUpdated || isFullUpdate;

                        if (isUpdating && namePlateKind == 0) {
                            var handler = new PlateUpdateHandler((nint)atkModule, args.Addon, reqArgs.NumberArrayData, reqArgs.StringArrayData, i);
                            Service.Log.Info($"  A[{i}] {handler.NamePlateInfo->Name} ({namePlateKind}) f[{updateFlags}/{(updateFlags & 1) != 0}]");
                        }
                    }
                }
                else if (type == AddonEvent.PostRequestedUpdate) {
                    //
                }
                break;
        }
    }

    public void Dispose()
    {
        nameplateHook?.Dispose();
        nameplateHookMinion?.Dispose();
        setPlayerNamePlateParentHook?.Dispose();
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "NamePlate", HandleEvent);
        Service.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "NamePlate", HandleEvent);
    }
}