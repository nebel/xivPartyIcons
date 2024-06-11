using System;
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
    private readonly StatusResolver _statusResolver;

    private const short ExIconWidth = 32;
    private const short ExIconWidthHalf = 16;
    private const short ExIconHeight = 32;
    private const short ExIconHeightHalf = 16;

    private const short ResNodeCenter = 144;
    private const short ResNodeBottom = 107;

    private const string FullWidthSpace = "　";

    public ZoneType ZoneType { get; set; }
    public NameplateMode PartyMode { get; set; }
    public NameplateMode OthersMode { get; set; }

    public DisplayConfig PartyDisplay { get; set; }
    public DisplayConfig OthersDisplay { get; set; }

    public StatusVisibility[] PartyStatus { get; set; }
    public StatusVisibility[] OthersStatus { get; set; }

    public NameplateView(RoleTracker roleTracker, Settings configuration, PlayerStylesheet stylesheet,
        StatusResolver statusResolver)
    {
        _roleTracker = roleTracker;
        _configuration = configuration;
        _stylesheet = stylesheet;
        _statusResolver = statusResolver;
    }

    public void Dispose()
    {
    }

    public void UpdateViewData(ref UpdateContext context)
    {
        var config = GetDisplayConfig(context);
        context.DisplayConfig = config;
        var mode = config.Mode;
        context.Mode = mode;

        if (mode is NameplateMode.Default or NameplateMode.Hide) {
            return;
        }

        var iconSetId = context.DisplayConfig.IconSetId;
        if (iconSetId == IconSetId.Inherit) {
            iconSetId = _configuration.IconSetId;
        }

        var genericRole = context.Job.GetRole();
        var iconSet = _stylesheet.GetGenericRoleIconGroupId(iconSetId, genericRole);
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
                var hasRole = _roleTracker.TryGetAssignedRole(context.PlayerCharacter, out var roleId);

                if (hasRole) {
                    var prefixString = new SeString()
                        .Append(_stylesheet.GetRolePlate(roleId))
                        .Append(" ");
                    prefix = SeStringUtils.SeStringToPtr(prefixString);
                }
                else {
                    prefix = SeStringUtils.EmptyPtr;
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
                if (PartyListHUDView.GetPartySlotIndex(context.PlayerCharacter.ObjectId) is { } partySlot) {
                    var slotString = _stylesheet.GetPartySlotNumber(partySlot + 1, context.GenericRole);
                    slotString.Payloads.Insert(0, new TextPayload(FullWidthSpace));
                    name = SeStringUtils.SeStringToPtr(slotString);
                }
                else {
                    name = SeStringUtils.EmptyPtr;
                }

                fcName = SeStringUtils.EmptyPtr;
                prefix = SeStringUtils.EmptyPtr;
                displayTitle = false;

                break;
            }
            case NameplateMode.RoleLetters:
            {
                var hasRole = _roleTracker.TryGetAssignedRole(context.PlayerCharacter, out var roleId);
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
                break;
            }
        }
    }

    public void ModifyNodes(PlateState state, UpdateContext context)
    {
        SetIconState(state, context.ShowExIcon, context.ShowSubIcon);

        state.CollisionScale = 1f;
        state.NeedsCollisionFix = true;

        switch (context.Mode) {
            case NameplateMode.Default:
            case NameplateMode.Hide:
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

        var iconNode = state.NamePlateObject->IconImageNode;
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

        exNode->LoadIconTexture((int)context.JobIconId, 0);

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
            subNode->LoadIconTexture((int)context.StatusIconId, 0);
        }
    }

    private static unsafe void PrepareNodeInlineLarge(PlateState state, UpdateContext context)
    {
        const short xAdjust = 4;
        const short yAdjust = -13;
        const float iconScale = 1.55f;

        var xAdjust2 = xAdjust;
        if (state.NamePlateObject->TextW > 50) {
            // Name is 3-wide including spacer, subtract an extra 1f scaled half-character width (+1 as a slight adjustment)
            xAdjust2 -= 19;
        }

        var iconConfig = context.DisplayConfig.ExIcon;

        var iconGroup = context.JobIconGroup;
        var iconPaddingRight = iconGroup.Padding.Right;

        var exNode = state.ExIconNode;
        exNode->AtkResNode.OriginX = ExIconWidth - iconPaddingRight;
        exNode->AtkResNode.OriginY = ExIconHeightHalf;

        var scale = iconGroup.Scale * iconScale * iconConfig.Scale;
        exNode->AtkResNode.SetScale(scale, scale);
        exNode->AtkResNode.SetPositionFloat(
            ResNodeCenter - ExIconWidth + iconPaddingRight + xAdjust2 + iconConfig.OffsetX,
            ResNodeBottom - ExIconHeight + yAdjust + iconConfig.OffsetY);

        exNode->LoadIconTexture((int)context.JobIconId, 0);

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

            subNode->AtkResNode.OriginX = 0 + subIconPaddingLeft;
            subNode->AtkResNode.OriginY = ExIconHeight - subIconPaddingBottom;
            subNode->AtkResNode.SetScale(subScale, subScale);
            subNode->AtkResNode.SetPositionFloat(
                ResNodeCenter + state.NamePlateObject->TextW - subIconPaddingLeft + subXAdjust + subIconConfig.OffsetX,
                ResNodeBottom - ExIconHeight + subIconPaddingBottom + subYAdjust + subIconConfig.OffsetY
            );
            subNode->LoadIconTexture((int)context.StatusIconId, 0);
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

        exNode->LoadIconTexture((int)context.JobIconId, 0);

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
        var resNode = state.NamePlateObject->ResNode;

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
}