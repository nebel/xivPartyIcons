using System;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.Sheets;
using PartyIcons.Configuration;
using PartyIcons.Stylesheet;
using System.Collections.Generic;

namespace PartyIcons.Runtime;

public sealed class ChatNameUpdater : IDisposable
{
    private readonly RoleTracker _roleTracker;
    private readonly PlayerStylesheet _stylesheet;

    public ChatConfig PartyMode { get; set; }
    public ChatConfig OthersMode { get; set; }

    public ChatNameUpdater(RoleTracker roleTracker, PlayerStylesheet stylesheet)
    {
        _roleTracker = roleTracker;
        _stylesheet = stylesheet;
    }

    public void Enable()
    {
        Service.ChatGui.ChatMessage += OnChatMessage;
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message,
        ref bool isHandled)
    {
        if (Service.ClientState.IsPvP) {
            return;
        }

        if (type is XivChatType.Say or XivChatType.Party or XivChatType.Alliance or XivChatType.Shout or XivChatType.Yell) {
            Parse(type, ref sender);
        }
    }

    public void Disable()
    {
        Service.ChatGui.ChatMessage -= OnChatMessage;
    }

    public void Dispose()
    {
        Disable();
    }

    private PlayerPayload? GetPlayerPayload(SeString sender)
    {
        var playerPayload = sender.Payloads.FirstOrDefault(p => p is PlayerPayload) as PlayerPayload ?? null;

        if (playerPayload == null) {
            playerPayload = new PlayerPayload(Service.PlayerState.CharacterName, Service.PlayerState.HomeWorld.RowId);
        }

        return playerPayload;
    }

    private bool CheckIfPlayerPayloadInParty(PlayerPayload playerPayload)
    {
        if (Plugin.Settings.TestingMode) {
            return true;
        }

        foreach (var member in Service.PartyList) {
            if (member.Name.ToString() == playerPayload.PlayerName && member.World.RowId == playerPayload.World.RowId) {
                return true;
            }
        }

        return false;
    }

    private record GroupPrefix(SeString Sender, TextPayload Payload, string Value)
    {
        public static GroupPrefix? Parse(SeString sender)
        {
            if (sender.Payloads.FirstOrDefault(p => p is TextPayload) as TextPayload is { Text: { } text } prefixPayload) {
                if (text.Length == 0)
                    return null;
                var prefix = text[..1];
                return prefix[0] is >= '\uE000' and <= '\uF8FF' ? new GroupPrefix(sender, prefixPayload, prefix) : null;
            }
            return null;
        }

        public void RemovePrefix()
        {
            Payload.Text = Payload.Text![1..];
            if (Payload.Text.Length == 0) {
                Sender.Payloads.Remove(Payload);
            }
        }
    }

    private void RemoveExistingForeground(SeString str)
    {
        str.Payloads.RemoveAll(p => p.Type == PayloadType.UIForeground);
    }

    private ClassJob? FindSenderJob(PlayerPayload playerPayload)
    {
        ClassJob? senderJob = null;

        foreach (var member in Service.PartyList) {
            if (member.Name.ToString() == playerPayload.PlayerName
                && member.World.RowId == playerPayload.World.RowId) {
                senderJob = member.ClassJob.ValueNullable;

                break;
            }
        }

        if (senderJob == null) {
            foreach (var obj in Service.ObjectTable) {
                if (obj is IPlayerCharacter pc
                    && pc.Name.ToString() == playerPayload.PlayerName
                    && pc.HomeWorld.RowId == playerPayload.World.RowId) {
                    senderJob = pc.ClassJob.ValueNullable;

                    break;
                }
            }
        }

        return senderJob is { RowId: 0 } ? null : senderJob;
    }

    private void Parse(XivChatType chatType, ref SeString sender)
    {
        if (GetPlayerPayload(sender) is not { } playerPayload)
            return;

        var isGroupChat = chatType is XivChatType.Party or XivChatType.Alliance;

        ChatConfig config;
        GroupPrefix? groupPrefix;
        if (isGroupChat) {
            config = PartyMode;
            groupPrefix = GroupPrefix.Parse(sender);
        }
        else {
            config = CheckIfPlayerPayloadInParty(playerPayload) ? PartyMode : OthersMode;
            groupPrefix = null;
        }

        if (config.Mode == ChatMode.Role && _roleTracker.TryGetAssignedRole(playerPayload.PlayerName, playerPayload.World.RowId, out var roleId)) {
            var customPrefix = new SeString();

            if (config.UseRoleColor) {
                RemoveExistingForeground(sender);
                customPrefix.Append(new UIForegroundPayload(_stylesheet.GetRoleChatColor(roleId)));
            }

            customPrefix.Append(_stylesheet.GetRoleChatPrefix(roleId));
            customPrefix.Append(new TextPayload(" "));

            sender.Payloads.InsertRange(0, customPrefix.Payloads);

            if (config.UseRoleColor) {
                sender.Payloads.Add(UIForegroundPayload.UIForegroundOff);
            }

            groupPrefix?.RemovePrefix();
        }
        else if (config.Mode != ChatMode.GameDefault)
        {
            if (FindSenderJob(playerPayload) is not { } senderJob)
                return;

            var customPrefix = new SeString();

            if (config.UseRoleColor) {
                RemoveExistingForeground(sender);
                customPrefix.Append(new UIForegroundPayload(_stylesheet.GetGenericRoleChatColor(senderJob)));
            }

            if (groupPrefix != null)
                customPrefix.Append(new TextPayload(groupPrefix.Value));
            customPrefix.Append(GetChatPrefix(config, senderJob));
            customPrefix.Append(new TextPayload(" "));

            sender.Payloads.InsertRange(0, customPrefix.Payloads);

            if (config.UseRoleColor) {
                sender.Payloads.Add(UIForegroundPayload.UIForegroundOff);
            }

            groupPrefix?.RemovePrefix();
        }
        else if (config is { Mode: ChatMode.GameDefault, UseRoleColor: true }) {
            if (FindSenderJob(playerPayload) is not { } senderJob)
                return;

            RemoveExistingForeground(sender);
            sender.Payloads.Insert(0, new UIForegroundPayload(_stylesheet.GetGenericRoleChatColor(senderJob)));
            sender.Payloads.Add(UIForegroundPayload.UIForegroundOff);
        }
    }

    private List<Payload> GetChatPrefix(ChatConfig config, ClassJob senderJob)
    {
        return config.Mode switch
        {
            ChatMode.Role => _stylesheet.GetGenericRoleChatPrefix(senderJob, config.UseRoleColor).Payloads,
            ChatMode.Job => _stylesheet.GetJobChatPrefix(senderJob, config.UseRoleColor).Payloads,
            _ => throw new ArgumentOutOfRangeException($"Cannot create chat prefix for mode {config.Mode}")
        };
    }
}