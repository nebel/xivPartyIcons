using System;
using Dalamud.Game.Text;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using PartyIcons.Entities;
using PartyIcons.Stylesheet;

namespace PartyIcons.Utils;

public unsafe class PartyListHUDView : IDisposable
{
    private readonly PlayerStylesheet _stylesheet;

    public PartyListHUDView(PlayerStylesheet stylesheet)
    {
        _stylesheet = stylesheet;
    }

    public void Dispose()
    {
        RevertSlotNumbers();
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

    public void DebugPartyData()
    {
        Service.Log.Info("======");

        var agentHud = AgentHUD.Instance();
        Service.Log.Info($"Members (AgentHud) [{agentHud->PartyMemberCount}]:");
        for (var i = 0; i < agentHud->PartyMemberListSpan.Length; i++) {
            var hudPartyMember = agentHud->PartyMemberListSpan[i];
            if (hudPartyMember.Name != null) {
                var name = MemoryHelper.ReadSeStringNullTerminated((nint)hudPartyMember.Name);
                Service.Log.Info($"  [{i}] {name} -> 0x{(nint)hudPartyMember.Object:X} ({(hudPartyMember.Object != null ? hudPartyMember.Object->Character.HomeWorld : "?")}) {hudPartyMember.ContentId} {hudPartyMember.ObjectId}");
            }
        }

        Service.Log.Info($"Members (PartyList) [{Service.PartyList.Length}]:");
        for (var i = 0; i < Service.PartyList.Length; i++) {
            var member = Service.PartyList[i];
            Service.Log.Info($"  [{i}] {member?.Name.TextValue ?? "?"} ({member?.World.Id}) {member?.ContentId}");
        }

        var proxy = InfoProxyParty.Instance();
        var list = proxy->InfoProxyCommonList;
        Service.Log.Info($"Members (Proxy) [{list.CharDataSpan.Length}]:");
        for (var i = 0; i < list.CharDataSpan.Length; i++) {
            var data = list.CharDataSpan[i];
            var name = MemoryHelper.ReadSeStringNullTerminated((nint)data.Name);
            Service.Log.Info($"  [{i}] {name} ({data.HomeWorld}) {data.ContentId}");
        }
    }
}
