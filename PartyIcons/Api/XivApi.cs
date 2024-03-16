using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace PartyIcons.Api;

public class XivApi : IDisposable
{
    public static void Initialize(Plugin plugin)
    {
        Instance ??= new XivApi();
    }

    private static XivApi? Instance;

    private XivApi()
    {
        Service.ClientState.Logout += OnLogout_ResetRaptureAtkModule;
    }

    public static void DisposeInstance() => Instance?.Dispose();

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
        private readonly AddonNamePlate.NamePlateObject* _ptr;

        private int _index;
        private SafeNamePlateInfo _namePlateInfo;

        public SafeNamePlateObject(AddonNamePlate.NamePlateObject* ptr, int index = -1)
        {
            _ptr = ptr;
            _index = index;
        }

        private int Index
        {
            get
            {
                if (_index == -1) {
                    _index = NamePlateArrayReader.GetIndexOf(_ptr);
                }

                return _index;
            }
        }

        public SafeNamePlateInfo NamePlateInfo
        {
            get
            {
                if (_namePlateInfo.ObjectID == default) {
                    // var raptureAtkModule = Framework.Instance()->GetUiModule()->GetRaptureAtkModule();
                    var rapturePtr = RaptureAtkModulePtr;
                    if (rapturePtr == IntPtr.Zero) {
                        Service.Log.Verbose($"[{GetType().Name}] RaptureAtkModule was null");
                        throw new Exception("Cannot get NamePlateInfo as RaptureAtkModule was null");
                    }

                    var raptureAtkModule = (RaptureAtkModule*)rapturePtr;
                    var infoArray = &raptureAtkModule->NamePlateInfoArray;
                    _namePlateInfo = new SafeNamePlateInfo(&infoArray[Index]);
                }

                return _namePlateInfo;
            }
        }

        public bool IsVisible => _ptr->IsVisible;

        public bool IsPlayer => _ptr->IsPlayerCharacter;

        /// <returns>True if the icon scale was changed.</returns>
        public bool SetIconScale(float scale, bool force = false)
        {
            if (force || !IsIconScaleEqual(scale)) {
                _ptr->IconImageNode->AtkResNode.SetScale(scale, scale);
                return true;
            }

            return false;
        }

        /// <returns>True if the name scale was changed.</returns>
        public bool SetNameScale(float scale, bool force = false)
        {
            if (force || !IsNameScaleEqual(scale)) {
                _ptr->NameText->AtkResNode.SetScale(scale, scale);
                return true;
            }

            return false;
        }

        public void SetIconPosition(short x, short y)
        {
            _ptr->IconXAdjust = x;
            _ptr->IconYAdjust = y;
        }

        private static bool NearlyEqual(float left, float right, float tolerance)
        {
            return Math.Abs(left - right) <= tolerance;
        }

        private bool IsIconScaleEqual(float scale)
        {
            var node = _ptr->IconImageNode->AtkResNode;
            return
                NearlyEqual(scale, node.ScaleX, ScaleTolerance) &&
                NearlyEqual(scale, node.ScaleY, ScaleTolerance);
        }

        private bool IsNameScaleEqual(float scale)
        {
            var node = _ptr->NameText->AtkResNode;
            return
                NearlyEqual(scale, node.ScaleX, ScaleTolerance) &&
                NearlyEqual(scale, node.ScaleY, ScaleTolerance);
        }

        private const float ScaleTolerance = 0.001f;
    }

    public readonly unsafe struct SafeNamePlateInfo(RaptureAtkModule.NamePlateInfo* pointer)
    {
        public readonly uint ObjectID = pointer->ObjectID.ObjectID;

        public bool IsPartyMember() => XivApi.IsPartyMember(ObjectID);

        public uint GetJobID() => GetJobId(ObjectID);
    }
}