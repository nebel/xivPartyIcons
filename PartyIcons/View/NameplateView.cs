﻿using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using PartyIcons.Utils;

namespace PartyIcons.View;

public sealed class NameplateView : IDisposable
{
    // [PluginService]
    // private ObjectTable ObjectTable { get; set; }

    private readonly Settings _configuration;
    private readonly PlayerStylesheet _stylesheet;
    private readonly RoleTracker _roleTracker;
    private readonly PartyListHUDView _partyListHudView;
    private ModeConfigs _modeConfigs = new();

    private const short ExIconWidth = 32;
    private const short ExIconWidthHalf = 16;
    private const short ExIconHeight = 32;
    private const short ExIconHeightHalf = 16;

    private const short ResNodeCenter = 144;
    private const short ResNodeBottom = 107;

    private const string FullWidthSpace = "　";

    public NameplateMode PartyMode { get; set; }
    public NameplateMode OthersMode { get; set; }

    public NameplateView(RoleTracker roleTracker, Settings configuration, PlayerStylesheet stylesheet,
        PartyListHUDView partyListHudView)
    {
        _roleTracker = roleTracker;
        _configuration = configuration;
        _stylesheet = stylesheet;
        _partyListHudView = partyListHudView;
    }

    public void Dispose()
    {
    }

    public void UpdateViewData(ref UpdateContext context)
    {
        var genericRole = context.Job.GetRole();
        var iconSet = _stylesheet.GetGenericRoleIconGroupId(genericRole);
        var iconGroup = IconRegistrar.Get(iconSet);

        context.JobIconGroup = iconGroup;
        context.JobIconId = iconGroup.GetJobIcon((uint)context.Job);
        context.StatusIconId = StatusUtils.OnlineStatusToIconId(context.Status);
        context.GenericRole = genericRole;
        context.Mode = GetModeForNameplate(context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void SetIconState(PlateState state, bool exIcon, bool subIcon)
    {
        state.ExIconNode->AtkResNode.ToggleVisibility(exIcon);
        state.UseExIcon = exIcon;

        state.SubIconNode->AtkResNode.ToggleVisibility(subIcon);
        state.UseSubIcon = subIcon;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void SetNameScale(PlateState state, float scale)
    {
        state.NamePlateObject->NameText->AtkResNode.SetScale(scale, scale);

        // var colScale = scale * 2;
        // var col = &state.NamePlateObject->CollisionNode1->AtkResNode;
        // // col->X = 0;
        // // col->Y = 0;
        // // col->X -= col->X * colScale;
        // // col->Y -= col->Y * colScale;
        // col->OriginX = col->Width / 2f;
        // col->OriginY = col->Height;
        // col->SetScale(colScale, colScale);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DoPriorityCheck(PlateState state, UpdateContext context)
    {
        if (IsPriorityStatus(context.Status) && state.UseExIcon) {
            (context.JobIconId, context.StatusIconId) = (context.StatusIconId, context.JobIconId);
            (context.JobIconGroup, context.StatusIconGroup) = (context.StatusIconGroup, context.JobIconGroup);
        }
    }

    public void ModifyParameters(UpdateContext context,
        ref bool isPrefixTitle,
        ref bool displayTitle,
        ref IntPtr title,
        ref IntPtr name,
        ref IntPtr fcName,
        ref IntPtr prefix,
        ref uint iconID)
    {
        var mode = context.Mode;

        var t = new SeString().Append("test");
        title = SeStringUtils.SeStringToPtr(t);


        if (_configuration.HideLocalPlayerNameplate && context.IsLocalPlayer) {
            if (mode == NameplateMode.RoleLetters && (_configuration.TestingMode || context.IsPartyMember))
                return;

            name = SeStringUtils.EmptyPtr;
            fcName = SeStringUtils.EmptyPtr;
            prefix = SeStringUtils.EmptyPtr;
            displayTitle = false;
            iconID = 0;
            return;
        }

        var playerCharacter = context.PlayerCharacter;

        switch (mode) {
            case NameplateMode.Default:
                break;
            case NameplateMode.Hide:
                name = SeStringUtils.EmptyPtr;
                fcName = SeStringUtils.EmptyPtr;
                prefix = SeStringUtils.EmptyPtr;
                displayTitle = false;
                iconID = 0;
                return;
            case NameplateMode.SmallJobIcon:
            {
                prefix = SeStringUtils.EmptyPtr;
                iconID = context.StatusIconId;
                break;
            }
            case NameplateMode.SmallJobIconAndRole:
            {
                var hasRole = _roleTracker.TryGetAssignedRole(playerCharacter.Name.TextValue, playerCharacter.HomeWorld.Id, out var roleId);
                if (hasRole) {
                    var prefixString = new SeString()
                        .Append(_stylesheet.GetRolePlate(roleId))
                        .Append(" ");
                    prefix = SeStringUtils.SeStringToPtr(prefixString);
                }
                else {
                    prefix = SeStringUtils.EmptyPtr;
                }

                iconID = context.StatusIconId;
                break;
            }
            case NameplateMode.BigJobIcon:
            {
                name = SeStringUtils.SeStringToPtr(SeStringUtils.Text(FullWidthSpace));
                fcName = SeStringUtils.EmptyPtr;
                prefix = SeStringUtils.EmptyPtr;
                displayTitle = false;
                iconID = 0;
                break;
            }
            case NameplateMode.BigJobIconAndPartySlot:
            {
                var partySlot = _partyListHudView.GetPartySlotIndex(context.PlayerCharacter.ObjectId) + 1;
                if (partySlot != null) {
                    var slotString = _stylesheet.GetPartySlotNumber(partySlot.Value, context.GenericRole);
                    slotString.Payloads.Insert(0, new TextPayload(FullWidthSpace));
                    name = SeStringUtils.SeStringToPtr(slotString);
                }
                else {
                    name = SeStringUtils.EmptyPtr;
                }

                fcName = SeStringUtils.EmptyPtr;
                prefix = SeStringUtils.EmptyPtr;
                displayTitle = false;
                iconID = 0;

                break;
            }
            case NameplateMode.RoleLetters:
            {
                var hasRole = _roleTracker.TryGetAssignedRole(playerCharacter.Name.TextValue, playerCharacter.HomeWorld.Id, out var roleId);
                var nameString = hasRole
                    ? _stylesheet.GetRolePlate(roleId)
                    : _stylesheet.GetGenericRolePlate(context.GenericRole);

                if (context.ShowExIcon) {
                    nameString.Payloads.Insert(0, new TextPayload(FullWidthSpace));
                }

                name = SeStringUtils.SeStringToPtr(nameString);
                prefix = SeStringUtils.EmptyPtr;
                fcName = SeStringUtils.EmptyPtr;
                displayTitle = false;
                iconID = 0;
                break;
            }
        }
    }

    public void ModifyNodes(PlateState state, UpdateContext context)
    {
        state.CollisionScale = 1f;
        state.NeedsCollisionFix = true;

        switch (context.Mode) {
            // case NameplateMode.Default:
            // case NameplateMode.Hide:
            //     SetIconState(state, false, false);
            //     SetNameScale(state, 0.5f);
            //     return;

            case NameplateMode.SmallJobIcon:
            case NameplateMode.SmallJobIconAndRole:
                SetIconState(state, true, false);
                SetNameScale(state, 0.5f);
                // DoPriorityCheck(state, context);
                PrepareNodeInlineSmall(state, context);
                // state.NeedsCollisionFix = true;
                break;

            case NameplateMode.BigJobIcon:
                SetIconState(state, true, true);
                SetNameScale(state, 1f);
                DoPriorityCheck(state, context);
                PrepareNodeCentered(state, context);
                // state.NeedsCollisionFix = true;
                break;

            case NameplateMode.BigJobIconAndPartySlot:
                SetIconState(state, true, true);
                SetNameScale(state, 1f);
                DoPriorityCheck(state, context);
                PrepareNodeInlineLarge(state, context);
                // state.NeedsCollisionFix = true;
                break;

            case NameplateMode.RoleLetters:
                SetIconState(state, context.ShowExIcon, context.ShowStatusIcon);
                SetNameScale(state, 1f);
                DoPriorityCheck(state, context);
                PrepareNodeInlineLarge(state, context);
                // state.NeedsCollisionFix = true;
                break;
        }
    }

    private static unsafe void PrepareNodeInlineSmall(PlateState state, UpdateContext context)
    {
        var iconGroup = context.JobIconGroup;
        var iconPaddingRight = iconGroup.Padding.Right;

        var exNode = state.ExIconNode;
        exNode->AtkResNode.OriginX = ExIconWidth - iconPaddingRight;
        exNode->AtkResNode.OriginY = ExIconHeightHalf;

        var scale = iconGroup.Scale;
        exNode->AtkResNode.SetScale(scale, scale);

        var iconNode = state.NamePlateObject->IconImageNode;
        if (state.IsIconBlank) {
            exNode->AtkResNode.SetPositionFloat(iconNode->AtkResNode.X - 6 + iconPaddingRight, iconNode->AtkResNode.Y);
        }
        else {
            exNode->AtkResNode.SetPositionFloat(iconNode->AtkResNode.X - 28 + iconPaddingRight, iconNode->AtkResNode.Y);
        }

        exNode->LoadIconTexture((int)context.JobIconId, 0);
    }

    private static unsafe void PrepareNodeInlineLarge(PlateState state, UpdateContext context)
    {
        const short xAdjust = 4;
        const short yAdjust = -13;
        const float iconScale = 1.55f;

        var xAdjust2 = xAdjust;
        if (state.NamePlateObject->TextW > 50) {
            // Name is 3-wide including spacer, subtract an extra scaled half-character width (+1 as a slight adjustment)
            xAdjust2 -= 19;
        }

        var iconGroup = context.JobIconGroup;
        var iconPaddingRight = iconGroup.Padding.Right;

        var exNode = state.ExIconNode;
        exNode->AtkResNode.OriginX = ExIconWidth - iconPaddingRight;
        exNode->AtkResNode.OriginY = ExIconHeightHalf;

        var scale = iconGroup.Scale * iconScale;
        exNode->AtkResNode.SetScale(scale, scale);
        exNode->AtkResNode.SetPositionFloat(ResNodeCenter - ExIconWidth + iconPaddingRight + xAdjust2 /*- iconWidthAdjust*/, ResNodeBottom - ExIconHeight + yAdjust);

        exNode->LoadIconTexture((int)context.JobIconId, 0);

        if (state.UseSubIcon) {
            const short subXAdjust = -10;
            const short subYAdjust = -5;
            const float subIconScale = 0.85f;

            var subNode = state.SubIconNode;
            var subIconGroup = context.StatusIconGroup;
            var subScale = subIconGroup.Scale * subIconScale;
            var subIconPaddingLeft = subIconGroup.Padding.Left;
            var subIconPaddingBottom = subIconGroup.Padding.Bottom;

            subNode->AtkResNode.OriginX = 0 + subIconPaddingLeft;
            subNode->AtkResNode.OriginY = ExIconHeight - subIconPaddingBottom;
            subNode->AtkResNode.SetScale(subScale, subScale);
            subNode->AtkResNode.SetPositionFloat( ResNodeCenter + state.NamePlateObject->TextW - subIconPaddingLeft + subXAdjust, ResNodeBottom - ExIconHeight + subIconPaddingBottom + subYAdjust);
            subNode->LoadIconTexture((int)context.StatusIconId, 0);
        }
    }

    private static unsafe void PrepareNodeCentered(PlateState state, UpdateContext context)
    {
        var iconGroup = context.JobIconGroup;

        const short yAdjust = -5;
        const float iconScale = 2.1f;
        state.CollisionScale = iconScale / 1.55f;

        var iconPaddingBottom = iconGroup.Padding.Bottom;

        var exNode = state.ExIconNode;
        exNode->AtkResNode.OriginX = ExIconWidthHalf;
        exNode->AtkResNode.OriginY = ExIconHeight - iconPaddingBottom;

        var scale = iconGroup.Scale * iconScale;
        exNode->AtkResNode.SetScale(scale, scale);
        exNode->AtkResNode.SetPositionFloat(ResNodeCenter - ExIconWidthHalf, ResNodeBottom - ExIconHeight + iconPaddingBottom + yAdjust);

        exNode->LoadIconTexture((int)context.JobIconId, 0);

        if (state.UseSubIcon) {
            const short subXAdjust = 6;
            const short subYAdjust = -5;
            const float subIconScale = 0.85f;

            var subNode = state.SubIconNode;
            var subIconGroup = context.StatusIconGroup;
            var subScale = subIconGroup.Scale * subIconScale;
            var subIconPaddingLeft = subIconGroup.Padding.Left;
            var subIconPaddingBottom = subIconGroup.Padding.Bottom;

            subNode->AtkResNode.OriginX = 0 + subIconPaddingLeft;
            subNode->AtkResNode.OriginY = ExIconHeight - subIconPaddingBottom;
            subNode->AtkResNode.SetScale(subScale, subScale);
            subNode->AtkResNode.SetPositionFloat(ResNodeCenter - subIconPaddingLeft + subXAdjust, ResNodeBottom - ExIconHeight + subIconPaddingBottom + subYAdjust);
            subNode->LoadIconTexture((int)context.StatusIconId, 0);
        }
    }

    private NameplateMode GetModeForNameplate(UpdateContext context)
    {
        if (_configuration.TestingMode || context.IsPartyMember) {
            return PartyMode;
        }

        return OthersMode;
    }

    private bool IsPriorityStatus(Status status)
    {
        if (_configuration.UsePriorityIcons == false && status != Status.Disconnected)
            return false;

        if (Plugin.ModeSetter.ZoneType == ZoneType.Foray)
            return StatusUtils.PriorityStatusesInForay.Contains(status);

        if (Plugin.ModeSetter.InDuty)
            return StatusUtils.PriorityStatusesInDuty.Contains(status);

        return false;
    }

    public unsafe void ModifyGlobalScale(PlateState state)
    {
        var resNode = state.NamePlateObject->ResNode;
        switch (_configuration.SizeMode) {
            case NameplateSizeMode.Smaller:
                resNode->OriginX = ResNodeCenter;
                resNode->OriginY = ResNodeBottom;
                resNode->SetScale(0.6f, 0.6f);
                state.IsGlobalScaleModified = true;
                break;
            case NameplateSizeMode.Bigger:
                resNode->OriginX = ResNodeCenter;
                resNode->OriginY = ResNodeBottom;
                resNode->SetScale(1.5f, 1.5f);
                state.IsGlobalScaleModified = true;
                break;
            case NameplateSizeMode.Medium:
            default:
                resNode->OriginX = 0;
                resNode->OriginY = 0;
                resNode->SetScale(1f, 1f);
                state.IsGlobalScaleModified = false;
                break;
        }
    }
}

public class ModeConfigs
{
    public PositionConfig SmallJobIcon;
    public PositionConfig SmallJobIconAndRole;
    public PositionConfig BigJobIcon;
    public PositionConfig BigJobIconAndPartySlot;
    public PositionConfig RoleLetters;

    public PositionConfig GetForMode(NameplateMode mode)
    {
        return mode switch
        {
            NameplateMode.Default => default,
            NameplateMode.Hide => default,
            NameplateMode.SmallJobIcon => SmallJobIcon,
            NameplateMode.SmallJobIconAndRole => SmallJobIconAndRole,
            NameplateMode.BigJobIcon => BigJobIcon,
            NameplateMode.BigJobIconAndPartySlot => BigJobIconAndPartySlot,
            NameplateMode.RoleLetters => RoleLetters,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}

public struct PositionConfig
{
    public float GlobalScale = 1;
    public IconConfig ExIconConfig = default;
    public IconConfig SubIconConfig = default;

    public PositionConfig()
    {
    }
}

public struct IconConfig
{
    public float Scale = 1f;
    public short OffsetX = 0;
    public short OffsetY = 0;

    public IconConfig()
    {
    }
}