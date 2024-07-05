using System;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.Interop;

namespace PartyIcons.Runtime;

public sealed class PartyStateTracker : IDisposable
{
    private readonly ulong[] _hudState = new ulong[8];
    private readonly PartySlot[] _partyState = new PartySlot[8];
    private short _lastPartySize;

    public bool InParty => _lastPartySize > 1;
    public short PartySize => _lastPartySize;

    public event Action<PartyChangeType>? OnPartyStateChange;

    public void Enable()
    {
        Service.Framework.Update += FrameworkOnUpdate;
    }

    public void Dispose()
    {
        Service.Framework.Update -= FrameworkOnUpdate;
    }

    private unsafe void FrameworkOnUpdate(IFramework framework)
    {
        var agentHud = AgentHUD.Instance();
        if (agentHud->PartyMemberCount == 0) return;

        var change = PartyChangeType.None;

        for (var i = 0; i < 8; i++) {
            var contentId = agentHud->PartyMembers.GetPointer(i)->ContentId;
            if (contentId > 0 && _hudState[i] != contentId) {
                _hudState[i] = contentId;
                change = PartyChangeType.Order;
            }
        }

        var gm = GroupManager.Instance();
        if (gm == null) return;

        var partySize = gm->MainGroup.MemberCount;

        for (var i = 0; i < partySize; i++) {
            var member = gm->MainGroup.PartyMembers.GetPointer(i);
            if (member == null) continue;

            var slot = _partyState[i];

            if (member->ContentId != slot.ContentId) {
                change = PartyChangeType.Member;
                _partyState[i] = new PartySlot(contentId: member->ContentId, job: member->ClassJob);
            }
            else if (member->ClassJob != slot.Job) {
                // Skip change notification for job changing to 0 (member moved out of range, which may be temporary)
                if (member->ClassJob != 0) {
                    change = change < PartyChangeType.Job ? PartyChangeType.Job : change;
                }
                _partyState[i] = new PartySlot(contentId: member->ContentId, job: member->ClassJob);
            }
        }

        if (_lastPartySize != partySize) {
            _lastPartySize = partySize;
            change = PartyChangeType.Member;
        }

        if (change != PartyChangeType.None) {
            Service.Log.Debug($"OnPartyChange ({change})");
            // PartyListHUDUpdater.DebugPartyData();
            OnPartyStateChange?.Invoke(change);
        }
    }
}

internal struct PartySlot(ulong contentId, byte job)
{
    public readonly ulong ContentId = contentId;
    public readonly byte Job = job;
}

public enum PartyChangeType : byte
{
    None,
    Order,
    Job,
    Member
}