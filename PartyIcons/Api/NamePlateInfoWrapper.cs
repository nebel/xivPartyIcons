using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace PartyIcons.Api;

public unsafe struct NamePlateInfoWrapper(RaptureAtkModule.NamePlateInfo* pointer)
{
    public readonly uint ObjectID = pointer->ObjectID.ObjectID;
    private PlayerCharacter? _character = null;

    private PlayerCharacter? Character
    {
        get
        {
            if (_character == null) {
                foreach (var obj in Service.ObjectTable) {
                    if (obj.ObjectId == ObjectID && obj is PlayerCharacter c) {
                        _character = c;
                        break;
                    }
                }
            }

            return _character;
        }
    }

    public bool IsPartyMember() => GroupManager.Instance()->IsObjectIDInParty(ObjectID);

    public uint GetJobID()
    {
        return Character?.ClassJob.Id ?? 0;
    }

    public OnlineStatus GetOnlineStatus()
    {
        return (OnlineStatus)(Character?.OnlineStatus.Id ?? 0);
    }

    public string GetOnlineStatusName()
    {
        return Character?.OnlineStatus.GameData?.Name.ToString() ?? "None";
    }
}