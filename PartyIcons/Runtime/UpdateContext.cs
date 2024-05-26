using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.Interop;
using PartyIcons.Api;
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
        Job = (Job)((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)playerCharacter.Address)->CharacterData
            .ClassJob;
        Status =
            (Status)((FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)playerCharacter.Address)->CharacterData
            .OnlineStatus;
    }
}