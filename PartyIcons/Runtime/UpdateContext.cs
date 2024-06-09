using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.View;

namespace PartyIcons.Runtime;

public unsafe class UpdateContext
{
    public override string ToString()
    {
        return
            $"{nameof(PlayerCharacter)}: {PlayerCharacter}, {nameof(IsLocalPlayer)}: {IsLocalPlayer}, {nameof(IsPartyMember)}: {IsPartyMember}, {nameof(Job)}: {Job}, {nameof(Status)}: {Status}, {nameof(JobIconId)}: {JobIconId}, {nameof(JobIconGroup)}: {JobIconGroup}, {nameof(StatusIconId)}: {StatusIconId}, {nameof(StatusIconGroup)}: {StatusIconGroup}, {nameof(GenericRole)}: {GenericRole}, {nameof(Mode)}: {Mode}, {nameof(DisplayConfig)}: {DisplayConfig}, {nameof(ShowExIcon)}: {ShowExIcon}, {nameof(ShowSubIcon)}: {ShowSubIcon}";
    }

    public readonly PlayerCharacter PlayerCharacter;
    public readonly bool IsLocalPlayer;
    public readonly bool IsPartyMember;
    public readonly Job Job;
    public readonly Status Status;
    public uint JobIconId;
    public IconGroup JobIconGroup = null!;
    public uint StatusIconId;
    public IconGroup StatusIconGroup = IconRegistrar.Status;
    public GenericRole GenericRole;
    public NameplateMode Mode;
    public DisplayConfig DisplayConfig;
    public bool ShowExIcon = true;
    public bool ShowSubIcon = true;

    public UpdateContext(PlayerCharacter playerCharacter)
    {
        var objectId = playerCharacter.ObjectId;
        PlayerCharacter = playerCharacter;
        IsLocalPlayer = objectId == Service.ClientState.LocalPlayer?.ObjectId;
        IsPartyMember = IsLocalPlayer || GroupManager.Instance()->IsObjectIDInParty(objectId);
        Job = (Job)((Character*)playerCharacter.Address)->CharacterData.ClassJob;
        Status = (Status)((Character*)playerCharacter.Address)->CharacterData.OnlineStatus;
    }
}