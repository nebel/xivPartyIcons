using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.View;

namespace PartyIcons.Runtime;

public unsafe class UpdateContext
{
    public readonly IPlayerCharacter PlayerCharacter;
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
    public DisplayConfig DisplayConfig = null!;
    public bool ShowExIcon = true;
    public bool ShowSubIcon = true;

    public UpdateContext(IPlayerCharacter playerCharacter)
    {
        var entityId = playerCharacter.EntityId;
        PlayerCharacter = playerCharacter;
        IsLocalPlayer = entityId == Service.ClientState.LocalPlayer?.EntityId;
        IsPartyMember = IsLocalPlayer || GroupManager.Instance()->MainGroup.IsEntityIdInParty(entityId);
        Job = (Job)((Character*)playerCharacter.Address)->CharacterData.ClassJob;
        Status = (Status)((Character*)playerCharacter.Address)->CharacterData.OnlineStatus;
    }
}