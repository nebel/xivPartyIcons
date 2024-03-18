using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace PartyIcons.Api;

public readonly unsafe struct NamePlateInfoWrapper(RaptureAtkModule.NamePlateInfo* pointer)
{
    public readonly uint ObjectID = pointer->ObjectID.ObjectID;

    public bool IsPartyMember() => GroupManager.Instance()->IsObjectIDInParty(ObjectID);

    public uint GetJobID()
    {
        foreach (var obj in Service.ObjectTable) {
            if (obj.ObjectId == ObjectID && obj is PlayerCharacter character) {
                return character.ClassJob.Id;
            }
        }

        return 0;
    }
}