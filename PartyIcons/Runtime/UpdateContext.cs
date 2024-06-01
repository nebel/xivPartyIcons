using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.View;

namespace PartyIcons.Runtime;

public unsafe class UpdateContext
{
    public readonly PlayerCharacter PlayerCharacter;
    public readonly bool IsLocalPlayer;
    public readonly bool IsPartyMember;
    public readonly Job Job;
    public readonly Status Status;
    public uint JobIconId;
    public IconGroup JobIconGroup = null!;
    public bool ShowJobIcon = true;
    public uint StatusIconId;
    public IconGroup StatusIconGroup = IconRegistrar.Status;
    public GenericRole GenericRole;
    public NameplateMode Mode;

    public UpdateContext(PlayerCharacter playerCharacter)
    {
        var objectId = playerCharacter.ObjectId;
        PlayerCharacter = playerCharacter;
        IsLocalPlayer = objectId == Service.ClientState.LocalPlayer?.ObjectId;
        IsPartyMember = IsLocalPlayer || GroupManager.Instance()->IsObjectIDInParty(objectId);
        Job = (Job)((Character*)playerCharacter.Address)->CharacterData.ClassJob;
        Status = (Status)((Character*)playerCharacter.Address)->CharacterData.OnlineStatus;
        // Service.Log.Info($"{playerCharacter.Name} -> {Status}");
    }
}