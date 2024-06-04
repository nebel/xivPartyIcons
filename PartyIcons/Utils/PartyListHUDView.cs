using System;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using PartyIcons.Entities;
using PartyIcons.Stylesheet;

namespace PartyIcons.Utils;

public sealed unsafe class PartyListHUDView : IDisposable
{
    private readonly PlayerStylesheet _stylesheet;

    public PartyListHUDView(PlayerStylesheet stylesheet)
    {
        _stylesheet = stylesheet;
    }

    public void Dispose()
    {
    }

    public static uint? GetPartySlotIndex(uint objectId)
    {
        var hud = AgentHUD.Instance();

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

    public void RevertPartyMemberRoleByIndex(AddonPartyList* addonPartyList, int index)
    {
        var memberStruct = addonPartyList->PartyMember[index];

        var nameNode = memberStruct.Name;
        nameNode->AtkResNode.SetPositionShort(19, 0);

        var numberNode = nameNode->AtkResNode.PrevSiblingNode->GetAsAtkTextNode();
        numberNode->AtkResNode.SetPositionShort(0, 0);
        numberNode->SetText(_stylesheet.BoxedCharacterString((index + 1).ToString()));
    }
}
