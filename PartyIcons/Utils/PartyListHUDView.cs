﻿using System;
using System.Text;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using PartyIcons.Entities;
using PartyIcons.Stylesheet;

namespace PartyIcons.Utils;

public unsafe class PartyListHUDView : IDisposable
{
    // [PluginService]
    // public PartyList PartyList { get; set; }

    private readonly PlayerStylesheet _stylesheet;
    private readonly IGameGui _gameGui;

    public PartyListHUDView(IGameGui gameGui, PlayerStylesheet stylesheet)
    {
        _gameGui = gameGui;
        _stylesheet = stylesheet;
    }

    public void Dispose()
    {
        RevertSlotNumbers();
    }

    public uint? GetPartySlotIndex(uint objectId)
    {
        var hud =
            FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule()->
                GetAgentHUD();

        if (hud == null)
        {
            Service.Log.Warning("AgentHUD null!");

            return null;
        }

        // 9 instead of 8 is used here, in case the player has a pet out
        if (hud->PartyMemberCount > 9)
        {
            // hud->PartyMemberCount gives out special (?) value when in trust
            Service.Log.Verbose("GetPartySlotIndex - trust detected, returning null");

            return null;
        }

        var list = (HudPartyMember*) hud->PartyMemberList;

        for (var i = 0; i < hud->PartyMemberCount; i++)
        {
            if (list[i].ObjectId == objectId)
            {
                return (uint) i;
            }
        }

        return null;
    }

    public void RevertSlotNumbers()
    {
        var addonPartyList = (AddonPartyList*) Service.GameGui.GetAddonByName("_PartyList", 1);
        if (addonPartyList == null)
        {
            return;
        }

        for (var i = 0; i < 8; i++) {
            RevertPartyMemberRoleByIndex(addonPartyList, i);
        }
    }

    public void RevertPartyMemberRoleByIndex(AddonPartyList* addonPartyList, int index)
    {
        var memberStruct = addonPartyList->PartyMember[index];

        var nameNode = memberStruct.Name;
        nameNode->AtkResNode.SetPositionShort(19, 0);

        var numberNode = nameNode->AtkResNode.PrevSiblingNode->GetAsAtkTextNode();
        numberNode->AtkResNode.SetPositionShort(0, 0);
        numberNode->SetText(_stylesheet.BoxedCharacterString((index + 1).ToString()));
    }

    public void SetPartyMemberRoleByIndex(AddonPartyList* addonPartyList, int index, RoleId roleId)
    {
        var memberStruct = addonPartyList->PartyMember[index];

        var nameNode = memberStruct.Name;
        nameNode->AtkResNode.SetPositionShort(29, 0);

        var numberNode = nameNode->AtkResNode.PrevSiblingNode->GetAsAtkTextNode();
        numberNode->AtkResNode.SetPositionShort(6, 0);

        var seString = _stylesheet.GetRolePlate(roleId);
        var buf = seString.Encode();

        fixed (byte* ptr = buf)
        {
            numberNode->SetText(ptr);
        }
    }

    public string GetDebugInfo()
    {
        var hud =
            FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule()->
                GetAgentHUD();

        if (hud == null)
        {
            Service.Log.Warning("AgentHUD null!");

            return null;
        }

        if (hud->PartyMemberCount > 9)
        {
            // hud->PartyMemberCount gives out special (?) value when in trust
            Service.Log.Verbose("GetPartySlotIndex - trust detected, returning null");

            return null;
        }

        var result = new StringBuilder();
        result.AppendLine($"PARTY ({Service.PartyList.Length}):");

        foreach (var member in Service.PartyList)
        {
            var index = GetPartySlotIndex(member.ObjectId);
            result.AppendLine(
                $"PartyList name {member.Name} oid {member.ObjectId} worldid {member.World.Id} slot index {index}");
        }

        result.AppendLine("STRUCTS:");
        var memberList = (HudPartyMember*) hud->PartyMemberList;

        for (var i = 0; i < Math.Min(hud->PartyMemberCount, 8u); i++)
        {
            var memberStruct = GetPartyMemberStruct((uint) i);

            if (memberStruct.HasValue)
            {
                /*
                for (var pi = 0; pi < memberStruct.Value.ClassJobIcon->PartsList->PartCount; pi++)
                {
                    var part = memberStruct.Value.ClassJobIcon->PartsList->Parts[pi];
                    result.Append($"icon {part.UldAsset->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName}");
                }
                */

                var strippedName = StripSpecialCharactersFromName(memberStruct.Value.Name->NodeText.ToString());
                result.AppendLine(
                    $"PartyMemberStruct index {i} name '{strippedName}', id matched {memberList[i].ObjectId}");

                var byteCount = 0;

                while (byteCount < 16 && memberList[i].Name[byteCount++] != 0) { }

                var memberListName = Encoding.UTF8.GetString(memberList[i].Name, byteCount - 1);
                result.AppendLine($"HudPartyMember index {i} name {memberListName} {memberList[i].ObjectId}");
            }
            else
            {
                result.AppendLine($"PartyMemberStruct null at {i}");
            }
        }

        return result.ToString();
    }

    private AddonPartyList.PartyListMemberStruct? GetPartyMemberStruct(uint idx)
    {
        var partyListAddon = (AddonPartyList*) _gameGui.GetAddonByName("_PartyList", 1);

        if (partyListAddon == null)
        {
            Service.Log.Warning("PartyListAddon null!");

            return null;
        }

        return idx switch
        {
            0 => partyListAddon->PartyMember.PartyMember0,
            1 => partyListAddon->PartyMember.PartyMember1,
            2 => partyListAddon->PartyMember.PartyMember2,
            3 => partyListAddon->PartyMember.PartyMember3,
            4 => partyListAddon->PartyMember.PartyMember4,
            5 => partyListAddon->PartyMember.PartyMember5,
            6 => partyListAddon->PartyMember.PartyMember6,
            7 => partyListAddon->PartyMember.PartyMember7,
            _ => throw new ArgumentException($"Invalid index: {idx}")
        };
    }

    private AddonPartyList.PartyListMemberStruct? GetPartyMemberStruct(AddonPartyList* addon, uint idx)
    {
        return idx switch
        {
            0 => addon->PartyMember.PartyMember0,
            1 => addon->PartyMember.PartyMember1,
            2 => addon->PartyMember.PartyMember2,
            3 => addon->PartyMember.PartyMember3,
            4 => addon->PartyMember.PartyMember4,
            5 => addon->PartyMember.PartyMember5,
            6 => addon->PartyMember.PartyMember6,
            7 => addon->PartyMember.PartyMember7,
            _ => throw new ArgumentException($"Invalid index: {idx}")
        };
    }

    private string StripSpecialCharactersFromName(string name)
    {
        var result = new StringBuilder();

        for (var i = 0; i < name.Length; i++)
        {
            var ch = name[i];

            if (ch >= 65 && ch <= 90 || ch >= 97 && ch <= 122 || ch == 45 || ch == 32 || ch == 39)
            {
                result.Append(name[i]);
            }
        }

        return result.ToString().Trim();
    }
}
