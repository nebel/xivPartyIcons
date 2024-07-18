using System;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.Interop;
using PartyIcons.Entities;
using PartyIcons.Stylesheet;

namespace PartyIcons.View;

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

    public static uint? GetPartySlotIndex(uint entityId)
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
            // TODO: ^ this is probably no longer be true
            Service.Log.Verbose("GetPartySlotIndex - trust detected, returning null");

            return null;
        }

        for (var i = 0; i < hud->PartyMemberCount; i++)
        {
            if (hud->PartyMembers.GetPointer(i)->EntityId == entityId)
            {
                return (uint)i;
            }
        }

        return null;
    }

    public void SetPartyMemberRoleByIndex(AddonPartyList* addonPartyList, int index, RoleId roleId)
    {
        var memberStruct = addonPartyList->PartyMembers.GetPointer(index);

        var nameNode = memberStruct->Name;
        nameNode->SetPositionShort(29, 0);

        var numberNode = nameNode->PrevSiblingNode->GetAsAtkTextNode();
        numberNode->SetPositionShort(6, 0);

        var seString = _stylesheet.GetRolePlate(roleId);
        var buf = seString.Encode();

        fixed (byte* ptr = buf)
        {
            numberNode->SetText(ptr);
        }
    }

    public void RevertPartyMemberRoleByIndex(AddonPartyList* addonPartyList, int index)
    {
        var memberStruct = addonPartyList->PartyMembers.GetPointer(index);

        var nameNode = memberStruct->Name;
        nameNode->SetPositionShort(19, 0);

        var numberNode = nameNode->PrevSiblingNode->GetAsAtkTextNode();
        numberNode->SetPositionShort(0, 0);
        numberNode->SetText(PlayerStylesheet.BoxedCharacterString((index + 1).ToString()));
    }
}
