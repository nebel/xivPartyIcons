using FFXIVClientStructs.FFXIV.Client.System.String;
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
    private readonly Utf8String*[] _strings = new Utf8String*[8];

    public PartyListHUDView(PlayerStylesheet stylesheet)
    {
        _stylesheet = stylesheet;
        for (var i = 0; i < 8; i++) {
            _strings[i] = Utf8String.CreateEmpty();
        }
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
        nameNode->AtkResNode.SetPositionShort(29, 0);

        var numberNode = nameNode->AtkResNode.PrevSiblingNode->GetAsAtkTextNode();
        numberNode->AtkResNode.SetPositionShort(6, 0);

        var seString = _stylesheet.GetRolePlate(roleId);

        var buf = _strings[index];
        buf->SetString(seString.EncodeWithNullTerminator());
        numberNode->SetText(buf->StringPtr);
    }

    public void RevertPartyMemberRoleByIndex(AddonPartyList* addonPartyList, int index)
    {
        var memberStruct = addonPartyList->PartyMembers.GetPointer(index);

        var nameNode = memberStruct->Name;
        nameNode->AtkResNode.SetPositionShort(19, 0);

        var numberNode = nameNode->AtkResNode.PrevSiblingNode->GetAsAtkTextNode();
        numberNode->AtkResNode.SetPositionShort(0, 0);

        var buf = _strings[index];
        buf->SetString(PlayerStylesheet.BoxedCharacterString((index + 1).ToString()));
        numberNode->SetText(buf->StringPtr);
    }

    public void FreeBufferByIndex(AddonPartyList* addonPartyList, int index)
    {
        var memberStruct = addonPartyList->PartyMembers.GetPointer(index);

        // Ensure original pointer is self-referential?!
        var nameNode = memberStruct->Name;
        nameNode->SetText(nameNode->NodeText.StringPtr);

        var buf = _strings[index];
        if (buf != null)
            buf->Dtor();
        _strings[index] = null;
    }
}
