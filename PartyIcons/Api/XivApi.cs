using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyIcons.Api;

public class XivApi : IDisposable
{
    public static int ThreadID => System.Threading.Thread.CurrentThread.ManagedThreadId;

    private static Plugin _plugin;

    public static void Initialize(Plugin plugin)
    {
        _plugin ??= plugin;
        Instance ??= new XivApi();
    }

    private static XivApi Instance;

    private XivApi()
    {
        Service.ClientState.Logout += OnLogout_ResetRaptureAtkModule;
    }

    public static void DisposeInstance() => Instance.Dispose();

    public void Dispose()
    {
        Service.ClientState.Logout -= OnLogout_ResetRaptureAtkModule;
    }

    #region RaptureAtkModule

    private static IntPtr _RaptureAtkModulePtr = IntPtr.Zero;

    public static IntPtr RaptureAtkModulePtr
    {
        get
        {
            if (_RaptureAtkModulePtr == IntPtr.Zero) {
                unsafe {
                    var framework = Framework.Instance();
                    var uiModule = framework->GetUiModule();

                    _RaptureAtkModulePtr = new IntPtr(uiModule->GetRaptureAtkModule());
                }
            }

            return _RaptureAtkModulePtr;
        }
    }

    private void OnLogout_ResetRaptureAtkModule() => _RaptureAtkModulePtr = IntPtr.Zero;

    #endregion

    public static bool IsLocalPlayer(uint actorID) => Service.ClientState.LocalPlayer?.ObjectId == actorID;

    public unsafe static bool IsPartyMember(uint actorID) =>
        FFXIVClientStructs.FFXIV.Client.Game.Group.GroupManager.Instance()->IsObjectIDInParty(actorID);

    public unsafe static bool IsAllianceMember(uint actorID) =>
        FFXIVClientStructs.FFXIV.Client.Game.Group.GroupManager.Instance()->IsObjectIDInParty(actorID);

    public static bool IsPlayerCharacter(uint actorID)
    {
        foreach (var obj in Service.ObjectTable) {
            if (obj == null) {
                continue;
            }

            if (obj.ObjectId == actorID) {
                return obj.ObjectKind == ObjectKind.Player;
            }
        }

        return false;
    }

    public static uint GetJobId(uint actorID)
    {
        foreach (var obj in Service.ObjectTable) {
            if (obj == null) {
                continue;
            }

            if (obj.ObjectId == actorID && obj is PlayerCharacter character) {
                return character.ClassJob.Id;
            }
        }

        return 0;
    }

    public unsafe class NamePlateArrayReader : IEnumerable<SafeNamePlateObject>
    {
        private readonly AddonNamePlate.NamePlateObject* _npObjectArrayPtr = GetObjectArrayPointer();
        private const int MaxNameplates = 50;

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
            var basePtr = (nint)GetObjectArrayPointer();
            var npObjectSize = Marshal.SizeOf(typeof(AddonNamePlate.NamePlateObject));
            var index = (int)((((nint)namePlateObjectPtr).ToInt64() - basePtr.ToInt64()) / npObjectSize);
            if (index is < 0 or >= MaxNameplates) {
                Service.Log.Verbose("NamePlateObject index was out of bounds");
                return -1;
            }

            return index;
        }

        private bool IsValid()
        {
            return _npObjectArrayPtr != null;
        }

        private SafeNamePlateObject Get(int index)
        {
            var ptr = &_npObjectArrayPtr[index];
            return new SafeNamePlateObject(ptr, index);
        }

        public IEnumerator<SafeNamePlateObject> GetEnumerator()
        {
            if (IsValid()) {
                for (var i = 0; i < MaxNameplates; i++) {
                    yield return Get(i);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public unsafe struct SafeNamePlateObject
    {
        private readonly AddonNamePlate.NamePlateObject* Pointer;

        private int _Index;
        private SafeNamePlateInfo _NamePlateInfo;

        public SafeNamePlateObject(AddonNamePlate.NamePlateObject* pointer, int index = -1)
        {
            Pointer = pointer;
            _Index = index;
        }

        private int Index
        {
            get
            {
                if (_Index == -1) {
                    _Index = NamePlateArrayReader.GetIndexOf(Pointer);
                }

                return _Index;
            }
        }

        public SafeNamePlateInfo NamePlateInfo
        {
            get
            {
                if (_NamePlateInfo == null) {
                    var rapturePtr = RaptureAtkModulePtr;

                    if (rapturePtr == IntPtr.Zero) {
                        Service.Log.Verbose($"[{GetType().Name}] RaptureAtkModule was null");
                        return null;
                    }

                    var npInfoArrayPtr = RaptureAtkModulePtr + Marshal.OffsetOf(typeof(RaptureAtkModule),
                        nameof(RaptureAtkModule.NamePlateInfoArray)).ToInt32();
                    var npInfoPtr = npInfoArrayPtr + Marshal.SizeOf(typeof(RaptureAtkModule.NamePlateInfo)) * Index;
                    _NamePlateInfo = new SafeNamePlateInfo(npInfoPtr);
                }

                return _NamePlateInfo;
            }
        }

        public bool IsVisible => Pointer->IsVisible;

        public bool IsPlayer => Pointer->IsPlayerCharacter;

        /// <returns>True if the icon scale was changed.</returns>
        public bool SetIconScale(float scale, bool force = false)
        {
            if (force || !IsIconScaleEqual(scale)) {
                Pointer->IconImageNode->AtkResNode.SetScale(scale, scale);
                return true;
            }

            return false;
        }

        /// <returns>True if the name scale was changed.</returns>
        public bool SetNameScale(float scale, bool force = false)
        {
            if (force || !IsNameScaleEqual(scale)) {
                Pointer->NameText->AtkResNode.SetScale(scale, scale);
                return true;
            }

            return false;
        }

        public void SetIconPosition(short x, short y)
        {
            Pointer->IconXAdjust = x;
            Pointer->IconXAdjust = y;
        }

        private static bool NearlyEqual(float left, float right, float tolerance)
        {
            return Math.Abs(left - right) <= tolerance;
        }

        private bool IsIconScaleEqual(float scale)
        {
            var node = Pointer->IconImageNode->AtkResNode;
            return
                NearlyEqual(scale, node.ScaleX, ScaleTolerance) &&
                NearlyEqual(scale, node.ScaleY, ScaleTolerance);
        }

        private bool IsNameScaleEqual(float scale)
        {
            var node = Pointer->NameText->AtkResNode;
            return
                NearlyEqual(scale, node.ScaleX, ScaleTolerance) &&
                NearlyEqual(scale, node.ScaleY, ScaleTolerance);
        }

        private const float ScaleTolerance = 0.001f;
    }

    public class SafeNamePlateInfo
    {
        public readonly IntPtr Pointer;
        public readonly RaptureAtkModule.NamePlateInfo Data;

        public SafeNamePlateInfo(IntPtr pointer)
        {
            Pointer = pointer; //-0x10;
            Data = Marshal.PtrToStructure<RaptureAtkModule.NamePlateInfo>(Pointer);
        }

        #region Getters

        public IntPtr NameAddress => GetStringPtr(nameof(RaptureAtkModule.NamePlateInfo.Name));

        public string Name => GetString(NameAddress);

        public IntPtr FcNameAddress => GetStringPtr(nameof(RaptureAtkModule.NamePlateInfo.FcName));

        public string FcName => GetString(FcNameAddress);

        public IntPtr TitleAddress => GetStringPtr(nameof(RaptureAtkModule.NamePlateInfo.Title));

        public string Title => GetString(TitleAddress);

        public IntPtr DisplayTitleAddress => GetStringPtr(nameof(RaptureAtkModule.NamePlateInfo.DisplayTitle));

        public string DisplayTitle => GetString(DisplayTitleAddress);

        public IntPtr LevelTextAddress => GetStringPtr(nameof(RaptureAtkModule.NamePlateInfo.LevelText));

        public string LevelText => GetString(LevelTextAddress);

        #endregion

        public bool IsPlayerCharacter() => XivApi.IsPlayerCharacter(Data.ObjectID.ObjectID);

        public bool IsPartyMember() => XivApi.IsPartyMember(Data.ObjectID.ObjectID);

        public bool IsAllianceMember() => XivApi.IsAllianceMember(Data.ObjectID.ObjectID);

        public uint GetJobID() => GetJobId(Data.ObjectID.ObjectID);

        private unsafe IntPtr GetStringPtr(string name)
        {
            var namePtr = Pointer + Marshal.OffsetOf(typeof(RaptureAtkModule.NamePlateInfo), name).ToInt32();
            var stringPtrPtr =
                namePtr + Marshal.OffsetOf(typeof(Utf8String), nameof(Utf8String.StringPtr)).ToInt32();
            var stringPtr = Marshal.ReadIntPtr(stringPtrPtr);

            return stringPtr;
        }

        private string GetString(IntPtr stringPtr) => Marshal.PtrToStringUTF8(stringPtr);
    }
}