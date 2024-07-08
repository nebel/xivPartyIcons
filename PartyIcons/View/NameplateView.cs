﻿using System;
using System.Runtime.CompilerServices;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using PartyIcons.Configuration;
using PartyIcons.Dalamud;
using PartyIcons.Entities;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;
using PartyIcons.Utils;

namespace PartyIcons.View;

public sealed class NameplateView : IDisposable
{
    private readonly Settings _configuration;
    private readonly PlayerStylesheet _stylesheet;
    private readonly RoleTracker _roleTracker;

    private const short ExIconWidth = 32;
    private const short ExIconWidthHalf = 16;
    private const short ExIconHeight = 32;
    private const short ExIconHeightHalf = 16;

    private const short ResNodeCenter = 144;
    private const short ResNodeBottom = 107;

    private const short TextIconSize = 18;
    private const string FullWidthSpace = "　";

    internal DisplayConfig PartyDisplay { get; private set; } = null!;
    internal DisplayConfig OthersDisplay { get; private set; } = null!;

    private StatusVisibility[] PartyStatus { get; set; } = null!;
    private StatusVisibility[] OthersStatus { get; set; } = null!;

    private const ZoneType DefaultZone = ZoneType.Overworld;
    private static readonly StatusSelector DefaultStatusSelector = new(DefaultZone);

    public NameplateView(RoleTracker roleTracker, Settings configuration, PlayerStylesheet stylesheet)
    {
        _roleTracker = roleTracker;
        _configuration = configuration;
        _stylesheet = stylesheet;

        SetZoneType(DefaultZone);
    }

    public void Dispose()
    {
    }

    public void SetZoneType(ZoneType zoneType)
    {
        var selectors = _configuration.DisplaySelectors;

        var partyDisplay = _configuration.GetDisplayConfig(zoneType switch
        {
            ZoneType.Overworld => selectors.DisplayOverworld,
            ZoneType.Dungeon => selectors.DisplayDungeon,
            ZoneType.Raid => selectors.DisplayRaid,
            ZoneType.AllianceRaid => selectors.DisplayAllianceRaid,
            ZoneType.FieldOperation => selectors.DisplayFieldOperationParty,
            _ => throw new ArgumentOutOfRangeException($"Unknown zone type {zoneType}")
        });

        var othersDisplay = _configuration.GetDisplayConfig(zoneType switch
        {
            ZoneType.Overworld => selectors.DisplayOthers,
            ZoneType.Dungeon => selectors.DisplayOthers,
            ZoneType.Raid => selectors.DisplayOthers,
            ZoneType.AllianceRaid => selectors.DisplayOthers,
            ZoneType.FieldOperation => selectors.DisplayFieldOperationOthers,
            _ => throw new ArgumentOutOfRangeException($"Unknown zone type {zoneType}")
        });

        PartyDisplay = partyDisplay;
        if (PartyDisplay.Mode is NameplateMode.Default or NameplateMode.Hide) {
            PartyStatus = StatusConfig.Defaults.None;
        }
        else {
            if (!partyDisplay.StatusSelectors.TryGetValue(zoneType, out var partyStatusSelector)) {
                Service.Log.Warning(
                    $"Couldn't find status selector for zoneType {zoneType} in config {partyDisplay.Preset}/{partyDisplay.Id}");
                partyStatusSelector = DefaultStatusSelector;
            }

            PartyStatus = StatusUtils.DictToArray(_configuration.GetStatusConfig(partyStatusSelector).DisplayMap);
        }

        OthersDisplay = othersDisplay;
        if (OthersDisplay.Mode is NameplateMode.Default or NameplateMode.Hide) {
            OthersStatus = StatusConfig.Defaults.None;
        }
        else {
            if (!othersDisplay.StatusSelectors.TryGetValue(zoneType, out var othersStatusSelector)) {
                Service.Log.Warning(
                    $"Couldn't find status selector for zoneType {zoneType} in config {othersDisplay.Preset}/{othersDisplay.Id}");
                othersStatusSelector = DefaultStatusSelector;
            }

            OthersStatus = StatusUtils.DictToArray(_configuration.GetStatusConfig(othersStatusSelector).DisplayMap);
        }
    }

    public void UpdateViewData(ref UpdateContext context)
    {
        var config = GetDisplayConfig(context);
        context.DisplayConfig = config;

        var mode = config.Mode;
        context.Mode = mode;

        if (_configuration.HideLocalPlayerNameplate && context.IsLocalPlayer) {
            if (mode == NameplateMode.RoleLetters && (_configuration.TestingMode || Service.PartyList.Length > 0)) {
                // Allow plate to draw since we're using RoleLetters and are in a party (or in testing mode)
            }
            else {
                context.Mode = NameplateMode.Hide;
            }
        }

        if (mode is NameplateMode.Default or NameplateMode.Hide) {
            return;
        }

        var iconSetId = context.DisplayConfig.IconSetId;
        if (iconSetId == IconSetId.Inherit) {
            iconSetId = _configuration.IconSetId;
        }

        var genericRole = context.Job.GetRole();
        var iconSet = PlayerStylesheet.GetGenericRoleIconGroupId(iconSetId, genericRole);
        var iconGroup = IconRegistrar.Get(iconSet);

        context.JobIconGroup = iconGroup;
        context.JobIconId = iconGroup.GetJobIcon((uint)context.Job);
        context.StatusIconId = StatusUtils.OnlineStatusToIconId(context.Status);
        context.GenericRole = genericRole;

        if (!config.ExIcon.Show) {
            context.ShowExIcon = false;
        }

        var statusLookup = GetStatusVisibilityForNameplate(context);
        var statusDisplay = statusLookup[(int)context.Status];

        if (context.Status == Status.None || statusDisplay == StatusVisibility.Hide || !config.SubIcon.Show) {
            context.ShowSubIcon = false;
        }
        else if (statusDisplay == StatusVisibility.Important) {
            switch (config.SwapStyle) {
                case StatusSwapStyle.None:
                    break;
                case StatusSwapStyle.Swap:
                    (context.JobIconId, context.StatusIconId) = (context.StatusIconId, context.JobIconId);
                    (context.JobIconGroup, context.StatusIconGroup) =
                        (context.StatusIconGroup, context.JobIconGroup);
                    (context.ShowExIcon, context.ShowSubIcon) = (context.ShowSubIcon, context.ShowExIcon);
                    break;
                case StatusSwapStyle.Replace:
                    context.JobIconId = context.StatusIconId;
                    context.JobIconGroup = context.StatusIconGroup;
                    context.ShowExIcon = true;
                    context.ShowSubIcon = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown swap style {config.SwapStyle}");
            }
        }
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
    }

    public void ModifyParameters(UpdateContext context,
        ref bool isPrefixTitle,
        ref bool displayTitle,
        ref IntPtr title,
        ref IntPtr name,
        ref IntPtr fcName,
        ref IntPtr prefix,
        ref uint iconId)
    {
        var mode = context.Mode;

        if (_configuration.HideLocalPlayerNameplate && context.IsLocalPlayer) {
            if (mode == NameplateMode.RoleLetters && (_configuration.TestingMode || Service.PartyList.Length > 0)) {
                // Allow plate to draw since we're using RoleLetters and are in a party (or in testing mode)
            }
            else {
                name = SeStringUtils.EmptyPtr;
                fcName = SeStringUtils.EmptyPtr;
                prefix = SeStringUtils.EmptyPtr;
                displayTitle = false;
                context.ShowExIcon = false;
                context.ShowSubIcon = false;
                return;
            }
        }

        switch (mode) {
            case NameplateMode.Default:
            default:
                throw new Exception(
                    $"Illegal state, should not enter {nameof(ModifyParameters)} with mode {context.Mode}");
            case NameplateMode.Hide:
                name = SeStringUtils.EmptyPtr;
                fcName = SeStringUtils.EmptyPtr;
                prefix = SeStringUtils.EmptyPtr;
                displayTitle = false;
                break;
            case NameplateMode.SmallJobIcon:
            {
                prefix = SeStringUtils.EmptyPtr;
                iconId = context.StatusIconId;
                break;
            }
            case NameplateMode.SmallJobIconAndRole:
            {
                prefix = SeStringUtils.EmptyPtr;
                if (context.DisplayConfig.RoleDisplayStyle == RoleDisplayStyle.PartyNumber) {
                    if (PartyListHUDView.GetPartySlotIndex(context.PlayerCharacter.EntityId) is { } partySlot) {
                        // var slotString = hasRole
                        //     ? _stylesheet.GetPartySlotNumber(partySlot + 1, roleId)
                        //     : _stylesheet.GetPartySlotNumber(partySlot + 1, context.GenericRole);
                        var slotString = _stylesheet.GetPartySlotNumber(partySlot + 1, context.GenericRole);
                        var prefixString = new SeString()
                            .Append(slotString)
                            .Append(" ");
                        prefix = SeStringUtils.SeStringToPtr(prefixString);
                    }
                }
                else {
                    if (_roleTracker.TryGetAssignedRole(context.PlayerCharacter, out var roleId)) {
                        var prefixString = new SeString()
                            .Append(_stylesheet.GetRolePlate(roleId))
                            .Append(" ");
                        prefix = SeStringUtils.SeStringToPtr(prefixString);
                    }
                }

                iconId = context.StatusIconId;
                break;
            }
            case NameplateMode.BigJobIcon:
            {
                name = SeStringUtils.SeStringToPtr(SeStringUtils.Text(FullWidthSpace));
                fcName = SeStringUtils.EmptyPtr;
                prefix = SeStringUtils.EmptyPtr;
                displayTitle = false;
                break;
            }
            case NameplateMode.BigJobIconAndPartySlot:
            {
                if (PartyListHUDView.GetPartySlotIndex(context.PlayerCharacter.EntityId) is { } partySlot) {
                    var slotString = _stylesheet.GetPartySlotNumber(partySlot + 1, context.GenericRole);
                    slotString.Payloads.Insert(0, new TextPayload(FullWidthSpace));
                    name = SeStringUtils.SeStringToPtr(slotString);
                }
                else {
                    name = SeStringUtils.SeStringToPtr(SeStringUtils.Text(FullWidthSpace));
                }

                fcName = SeStringUtils.EmptyPtr;
                prefix = SeStringUtils.EmptyPtr;
                displayTitle = false;

                break;
            }
            case NameplateMode.RoleLetters:
            {
                SeString nameString;
                if (context.DisplayConfig.RoleDisplayStyle == RoleDisplayStyle.PartyNumber) {
                    nameString = PartyListHUDView.GetPartySlotIndex(context.PlayerCharacter.EntityId) is { } partySlot
                        ? _stylesheet.GetPartySlotNumber(partySlot + 1, context.GenericRole)
                        : _stylesheet.GetGenericRolePlate(context.GenericRole);
                }
                else {
                    var hasRole = _roleTracker.TryGetAssignedRole(context.PlayerCharacter, out var roleId);
                    nameString = hasRole
                        ? _stylesheet.GetRolePlate(roleId)
                        : _stylesheet.GetGenericRolePlate(context.GenericRole);
                }

                if (context.ShowExIcon) {
                    nameString.Payloads.Insert(0, new TextPayload(FullWidthSpace));
                }

                name = SeStringUtils.SeStringToPtr(nameString);
                prefix = SeStringUtils.EmptyPtr;
                fcName = SeStringUtils.EmptyPtr;
                displayTitle = false;
                break;
            }
        }
    }

    public void DoPendingChanges(PlateState state)
    {
        var context = state.PendingChangesContext;
        if (context == null) return;

        state.PendingChangesContext = null;
        ModifyNodes(state, context);
        ModifyGlobalScale(state, context);
        // unsafe {
        //     state.NamePlateObject->NameContainer->DrawFlags |= 1;
        // }
    }

    public void ModifyNodes(PlateState state, UpdateContext context)
    {
        SetIconState(state, context.ShowExIcon, context.ShowSubIcon);

        state.CollisionScale = 1f;
        state.NeedsCollisionFix = true;

        switch (context.Mode) {
            case NameplateMode.Default:
            case NameplateMode.Hide:
            default:
                throw new Exception($"Illegal state, should not enter {nameof(ModifyNodes)} with mode {context.Mode}");

            case NameplateMode.SmallJobIcon:
            case NameplateMode.SmallJobIconAndRole:
                SetNameScale(state, 0.5f);
                PrepareNodeInlineSmall(state, context);
                break;

            case NameplateMode.BigJobIcon:
                SetNameScale(state, 1f);
                PrepareNodeCentered(state, context);
                break;

            case NameplateMode.BigJobIconAndPartySlot:
            case NameplateMode.RoleLetters:
                SetNameScale(state, 1f);
                PrepareNodeInlineLarge(state, context);
                break;
        }
    }

    private static unsafe void PrepareNodeInlineSmall(PlateState state, UpdateContext context)
    {
        var iconConfig = context.DisplayConfig.ExIcon;

        var iconGroup = context.JobIconGroup;
        var iconPaddingRight = iconGroup.Padding.Right;

        var exNode = state.ExIconNode;
        exNode->AtkResNode.OriginX = ExIconWidth - iconPaddingRight;
        exNode->AtkResNode.OriginY = ExIconHeightHalf;

        var scale = iconGroup.Scale * iconConfig.Scale;
        exNode->AtkResNode.SetScale(scale, scale);

        var iconNode = state.NamePlateObject->NameIcon;
        if (context.ShowSubIcon) {
            exNode->AtkResNode.SetPositionFloat(
                iconNode->AtkResNode.X - 28 + iconPaddingRight + iconConfig.OffsetX,
                iconNode->AtkResNode.Y + iconConfig.OffsetY
            );
        }
        else {
            exNode->AtkResNode.SetPositionFloat(
                iconNode->AtkResNode.X - 6 + iconPaddingRight + iconConfig.OffsetX,
                iconNode->AtkResNode.Y + iconConfig.OffsetY
            );
        }

        exNode->LoadIconTexture(context.JobIconId, 0);

        if (state.UseSubIcon) {
            const short subXAdjust = -4;
            const short subYAdjust = 0;
            const float subIconScale = 0.8f;

            var subIconConfig = context.DisplayConfig.SubIcon;

            var subNode = state.SubIconNode;
            var subIconGroup = context.StatusIconGroup;
            var subScale = subIconGroup.Scale * subIconScale * subIconConfig.Scale;
            var subIconPaddingRight = subIconGroup.Padding.Right;

            subNode->AtkResNode.OriginX = ExIconWidth - subIconPaddingRight;
            subNode->AtkResNode.OriginY = ExIconHeight / 2f;
            subNode->AtkResNode.SetScale(subScale, subScale);
            subNode->AtkResNode.SetPositionFloat(
                iconNode->AtkResNode.X + subIconPaddingRight + subXAdjust + subIconConfig.OffsetX,
                iconNode->AtkResNode.Y + subYAdjust + subIconConfig.OffsetY
            );
            subNode->LoadIconTexture(context.StatusIconId, 0);
        }
    }

    private static unsafe void PrepareNodeInlineLarge(PlateState state, UpdateContext context)
    {
        const short xAdjust = 4;
        const short yAdjust = -13;
        const float iconScale = 1.55f;

        var iconConfig = context.DisplayConfig.ExIcon;

        var iconGroup = context.JobIconGroup;
        var iconPaddingRight = iconGroup.Padding.Right;

        var exNode = state.ExIconNode;
        exNode->AtkResNode.OriginX = ExIconWidth - iconPaddingRight;
        exNode->AtkResNode.OriginY = ExIconHeightHalf;

        var scale = iconGroup.Scale * iconScale * iconConfig.Scale;
        exNode->AtkResNode.SetScale(scale, scale);

        var iconTextWidthAdjust = TextIconSize - Math.Max(0, state.NamePlateObject->TextW / TextIconSize - 1) * TextIconSize;
        exNode->AtkResNode.SetPositionFloat(
            ResNodeCenter - ExIconWidth + iconPaddingRight + iconTextWidthAdjust + xAdjust + iconConfig.OffsetX,
            ResNodeBottom - ExIconHeight + yAdjust + iconConfig.OffsetY);

        exNode->LoadIconTexture(context.JobIconId, 0);

        if (state.UseSubIcon) {
            const short subXAdjust = -10;
            const short subYAdjust = -5;
            const float subIconScale = 0.85f;

            var subIconConfig = context.DisplayConfig.SubIcon;

            var subNode = state.SubIconNode;
            var subIconGroup = context.StatusIconGroup;
            var subScale = subIconGroup.Scale * subIconScale * subIconConfig.Scale;
            var subIconPaddingLeft = subIconGroup.Padding.Left;
            var subIconPaddingBottom = subIconGroup.Padding.Bottom;

            var textW = state.NamePlateObject->TextW;
            if (state.NamePlateObject->TextW == 0) {
                // There is no text, so treat the icon (which presumably does exist...) as text.
                textW += TextIconSize;
            }

            subNode->AtkResNode.OriginX = 0 + subIconPaddingLeft;
            subNode->AtkResNode.OriginY = ExIconHeight - subIconPaddingBottom;
            subNode->AtkResNode.SetScale(subScale, subScale);
            subNode->AtkResNode.SetPositionFloat(
                ResNodeCenter + textW - subIconPaddingLeft + subXAdjust + subIconConfig.OffsetX,
                ResNodeBottom - ExIconHeight + subIconPaddingBottom + subYAdjust + subIconConfig.OffsetY
            );
            subNode->LoadIconTexture(context.StatusIconId, 0);
        }
    }

    private static unsafe void PrepareNodeCentered(PlateState state, UpdateContext context)
    {
        var iconConfig = context.DisplayConfig.ExIcon;

        var iconGroup = context.JobIconGroup;

        const short yAdjust = -5;
        const float iconScale = 2.1f;

        var iconScale2 = iconScale * iconConfig.Scale;
        state.CollisionScale = iconScale2 / 1.55f;

        var iconPaddingBottom = iconGroup.Padding.Bottom;

        var exNode = state.ExIconNode;
        exNode->AtkResNode.OriginX = ExIconWidthHalf;
        exNode->AtkResNode.OriginY = ExIconHeight - iconPaddingBottom;

        var scale = iconGroup.Scale * iconScale2;
        exNode->AtkResNode.SetScale(scale, scale);
        exNode->AtkResNode.SetPositionFloat(
            ResNodeCenter - ExIconWidthHalf + iconConfig.OffsetX,
            ResNodeBottom - ExIconHeight + iconPaddingBottom + yAdjust + iconConfig.OffsetY
        );

        exNode->LoadIconTexture(context.JobIconId, 0);

        if (state.UseSubIcon) {
            const short subXAdjust = 6;
            const short subYAdjust = -5;
            const float subIconScale = 0.85f;

            var subIconConfig = context.DisplayConfig.SubIcon;

            var subNode = state.SubIconNode;
            var subIconGroup = context.StatusIconGroup;
            var subScale = subIconGroup.Scale * subIconScale * subIconConfig.Scale;
            var subIconPaddingLeft = subIconGroup.Padding.Left;
            var subIconPaddingBottom = subIconGroup.Padding.Bottom;

            subNode->AtkResNode.OriginX = 0 + subIconPaddingLeft;
            subNode->AtkResNode.OriginY = ExIconHeight - subIconPaddingBottom;
            subNode->AtkResNode.SetScale(subScale, subScale);
            subNode->AtkResNode.SetPositionFloat(
                ResNodeCenter - subIconPaddingLeft + subXAdjust + subIconConfig.OffsetX,
                ResNodeBottom - ExIconHeight + subIconPaddingBottom + subYAdjust + subIconConfig.OffsetY
            );
            subNode->LoadIconTexture(context.StatusIconId, 0);
        }
    }

    private DisplayConfig GetDisplayConfig(UpdateContext context)
    {
        if (_configuration.TestingMode || context.IsPartyMember) {
            return PartyDisplay;
        }

        return OthersDisplay;
    }

    private StatusVisibility[] GetStatusVisibilityForNameplate(UpdateContext context)
    {
        if (_configuration.TestingMode || context.IsPartyMember) {
            return PartyStatus;
        }

        return OthersStatus;
    }

    public unsafe void ModifyGlobalScale(PlateState state, UpdateContext context)
    {
        var resNode = state.NamePlateObject->NameContainer;

        var scale = _configuration.SizeMode switch
        {
            NameplateSizeMode.Smaller => 0.6f,
            NameplateSizeMode.Medium => 1f,
            NameplateSizeMode.Bigger => 1.5f,
            NameplateSizeMode.Custom => _configuration.SizeModeCustom,
            _ => 1f
        };

        scale *= context.DisplayConfig.Scale;

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (scale == 1f) {
            // Service.Log.Info("SetScale: None");
            resNode->OriginX = 0;
            resNode->OriginY = 0;
            resNode->SetScale(1f, 1f);
            state.IsGlobalScaleModified = false;
        }
        else {
            // Service.Log.Info($"SetScale: {scale}");
            resNode->OriginX = ResNodeCenter;
            resNode->OriginY = ResNodeBottom;
            resNode->SetScale(scale, scale);
            state.IsGlobalScaleModified = true;
        }
    }

    private const int EmptyIconId = -1;
    private const int PlaceholderEmptyIconId = 61696;

    public void ModifyPlateData(UpdateContext context, PlateUpdateHandler handler)
    {
        var mode = context.Mode;

        // Replace 0/-1 with empty dummy texture so the default icon is always positioned even for unselected
        // targets (when unselected targets are hidden). If we don't do this, the icon node will only be
        // positioned by the game after the target is selected for hidden nameplates, which would force us to
        // re-position after the initial SetNamePlate call (which would be very annoying).
        handler.NameIconId = PlaceholderEmptyIconId;

        switch (mode) {
            case NameplateMode.Default:
            default:
                throw new Exception(
                    $"Illegal state, should not enter {nameof(ModifyPlateData)} with mode {context.Mode}");
            case NameplateMode.Hide:
                handler.ClearField(NamePlateStringField.Name);
                handler.ClearField(NamePlateStringField.FreeCompanyTag);
                handler.ClearField(NamePlateStringField.StatusPrefix);
                handler.DisplayTitle = false;
                break;
            case NameplateMode.SmallJobIcon:
            {
                handler.ClearField(NamePlateStringField.StatusPrefix);
                break;
            }
            case NameplateMode.SmallJobIconAndRole:
            {
                if (context.DisplayConfig.RoleDisplayStyle == RoleDisplayStyle.PartyNumber) {
                    if (PartyListHUDView.GetPartySlotIndex(context.PlayerCharacter.EntityId) is { } partySlot) {
                        // var slotString = hasRole
                        //     ? _stylesheet.GetPartySlotNumber(partySlot + 1, roleId)
                        //     : _stylesheet.GetPartySlotNumber(partySlot + 1, context.GenericRole);
                        var slotString = _stylesheet.GetPartySlotNumber(partySlot + 1, context.GenericRole);
                        var prefixString = new SeString()
                            .Append(slotString)
                            .Append(" ");
                        handler.SetField(NamePlateStringField.StatusPrefix, prefixString);
                    }
                    else {
                        handler.ClearField(NamePlateStringField.StatusPrefix);
                    }
                }
                else {
                    if (_roleTracker.TryGetAssignedRole(context.PlayerCharacter, out var roleId)) {
                        var prefixString = new SeString()
                            .Append(_stylesheet.GetRolePlate(roleId))
                            .Append(" ");
                        handler.SetField(NamePlateStringField.StatusPrefix, prefixString);
                    }
                    else {
                        handler.ClearField(NamePlateStringField.StatusPrefix);
                    }
                }

                break;
            }
            case NameplateMode.BigJobIcon:
            {
                handler.SetField(NamePlateStringField.Name, SeStringUtils.Text(FullWidthSpace));
                handler.ClearField(NamePlateStringField.FreeCompanyTag);
                handler.ClearField(NamePlateStringField.StatusPrefix);
                handler.DisplayTitle = false;
                break;
            }
            case NameplateMode.BigJobIconAndPartySlot:
            {
                if (PartyListHUDView.GetPartySlotIndex(context.PlayerCharacter.EntityId) is { } partySlot) {
                    var slotString = _stylesheet.GetPartySlotNumber(partySlot + 1, context.GenericRole);
                    slotString.Payloads.Insert(0, new TextPayload(FullWidthSpace));
                    handler.SetField(NamePlateStringField.Name, slotString);
                }
                else {
                    handler.SetField(NamePlateStringField.Name, SeStringUtils.Text(FullWidthSpace));
                }

                handler.ClearField(NamePlateStringField.FreeCompanyTag);
                handler.ClearField(NamePlateStringField.StatusPrefix);
                handler.DisplayTitle = false;

                break;
            }
            case NameplateMode.RoleLetters:
            {
                SeString nameString;
                if (context.DisplayConfig.RoleDisplayStyle == RoleDisplayStyle.PartyNumber) {
                    nameString = PartyListHUDView.GetPartySlotIndex(context.PlayerCharacter.EntityId) is { } partySlot
                        ? _stylesheet.GetPartySlotNumber(partySlot + 1, context.GenericRole)
                        : _stylesheet.GetGenericRolePlate(context.GenericRole);
                }
                else {
                    var hasRole = _roleTracker.TryGetAssignedRole(context.PlayerCharacter, out var roleId);
                    nameString = hasRole
                        ? _stylesheet.GetRolePlate(roleId)
                        : _stylesheet.GetGenericRolePlate(context.GenericRole);
                }

                if (context.ShowExIcon) {
                    nameString.Payloads.Insert(0, new TextPayload(FullWidthSpace));
                }

                handler.SetField(NamePlateStringField.Name, nameString);
                handler.ClearField(NamePlateStringField.FreeCompanyTag);
                handler.ClearField(NamePlateStringField.StatusPrefix);
                handler.DisplayTitle = false;
                break;
            }
        }
    }
}