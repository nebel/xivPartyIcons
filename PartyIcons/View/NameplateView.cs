﻿using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PartyIcons.Api;
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

    private const short ExIconWidth = 32;
    private const short ExIconHeight = 32;

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

    /// <returns>True if the icon or name scale was changed (different from default).</returns>
    public bool SetupDefault(NamePlateObjectWrapper npObject)
    {
        var iconScaleChanged = npObject.SetIconScale(1f);
        var nameScaleChanged = npObject.SetNameScale(0.5f);
        return iconScaleChanged || nameScaleChanged;
    }

    private static unsafe void SetUseExIcon(NameplateUpdater.PlateInfo info, bool value)
    {
        // if (value) {
        //     info.ExIconNode->AtkResNode.NodeFlags |= NodeFlags.Visible;
        // }
        // else {
        //     info.ExIconNode->AtkResNode.NodeFlags &= NodeFlags.Visible;
        // }

        info.ExIconNode->AtkResNode.ToggleVisibility(value);
        info.UseExIcon = value;
    }

    /// <summary>
    /// Position and scale nameplate elements based on the current mode.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="npObject">The nameplate object to modify.</param>
    /// <param name="forceIcon">Whether modes that do not normally support icons should be adjusted to show an icon.</param>
    public void SetupForPC(NameplateUpdater.PlateInfo info, NamePlateObjectWrapper npObject, bool forceIcon)
    {
        var nameScale = 0.5f;
        var iconScale = 1f;
        var iconOffset = new Vector2(0, 0);

        switch (GetModeForNameplate(npObject)) {
            case NameplateMode.Default:
                SetUseExIcon(info, false);
                SetupDefault(npObject);
                return;

            case NameplateMode.SmallJobIcon:
            case NameplateMode.SmallJobIconAndRole:
                SetUseExIcon(info, true);
                SetupDefault(npObject);
                break;

            case NameplateMode.Hide:
                SetUseExIcon(info, false);
                nameScale = 0f;
                iconScale = 0f;
                break;

            case NameplateMode.BigJobIcon:
                SetUseExIcon(info, true);
                nameScale = 0.75f;

                switch (_configuration.SizeMode) {
                    case NameplateSizeMode.Smaller:
                        iconOffset = new Vector2(9, 50);
                        iconScale = 1.5f;

                        break;

                    case NameplateSizeMode.Medium:
                        iconOffset = new Vector2(-12, 24);
                        iconScale = 3f;

                        break;

                    case NameplateSizeMode.Bigger:
                        iconOffset = new Vector2(-27, -12);
                        iconScale = 4f;

                        break;
                }

                break;

            case NameplateMode.BigJobIconAndPartySlot:
                SetUseExIcon(info, true);
                switch (_configuration.SizeMode) {
                    case NameplateSizeMode.Smaller:
                        iconOffset = new Vector2(12, 62);
                        iconScale = 1.2f;
                        nameScale = 0.6f;

                        break;

                    case NameplateSizeMode.Medium:
                        iconOffset = new Vector2(-14, 41);
                        iconScale = 2.3f;
                        nameScale = 1f;

                        break;

                    case NameplateSizeMode.Bigger:
                        iconOffset = new Vector2(-32, 15);
                        iconScale = 3f;
                        nameScale = 1.5f;

                        break;
                }

                break;

            case NameplateMode.RoleLetters:
                // SetUseExIcon(info, false);
                SetUseExIcon(info, true);
                iconScale = 0f;

                // Allow an icon to be displayed in roles only mode
                if (forceIcon) {
                    iconScale = _configuration.SizeMode switch
                    {
                        NameplateSizeMode.Smaller => 1f,
                        NameplateSizeMode.Medium => 1.5f,
                        NameplateSizeMode.Bigger => 2f
                    };
                    iconOffset = _configuration.SizeMode switch
                    {
                        NameplateSizeMode.Smaller => new Vector2(-6, 74),
                        NameplateSizeMode.Medium => new Vector2(-42, 55),
                        NameplateSizeMode.Bigger => new Vector2(-78, 35)
                    };
                }

                nameScale = _configuration.SizeMode switch
                {
                    NameplateSizeMode.Smaller => 0.5f,
                    NameplateSizeMode.Medium => 1f,
                    NameplateSizeMode.Bigger => 1.5f
                };

                break;
        }

        // var iconGroup = GetIconGroup(npObject.NamePlateInfo);
        // var jobId = npObject.NamePlateInfo.GetJobID();
        // unsafe {
        //     info.ExIconNode->LoadIconTexture((int)iconGroup.GetJobIcon(jobId), 0);
        //     Service.Log.Warning($"scale: {iconGroup.Scale}");
        //     info.ExIconNode->AtkResNode.SetScale(iconGroup.Scale, iconGroup.Scale);
        // }

        // npObject.SetIconPosition((short)iconOffset.X, (short)iconOffset.Y);
        // npObject.SetIconScale(iconScale);
        npObject.SetNameScale(nameScale);
    }

    public unsafe void PrepareNodeInline(NameplateUpdater.PlateInfo info, NamePlateObjectWrapper wrapper)
    {
        var iconGroup = GetIconGroup(wrapper.NamePlateInfo);

        const short exWidth = 32;
        const short exHeight = 32;
        var iconPaddingRight = iconGroup.Padding.Right;

        var exNode = info.ExIconNode;
        exNode->AtkResNode.OriginX = exWidth - iconPaddingRight;
        exNode->AtkResNode.OriginY = exHeight / 2f;

        var scale = iconGroup.Scale;
        exNode->AtkResNode.SetScale(scale, scale);

        var iconNode = info.IconNode;
        if (info.IsBlankIcon) {
            exNode->AtkResNode.SetPositionFloat(iconNode->AtkResNode.X - 6 + iconPaddingRight,
                iconNode->AtkResNode.Y);
        }
        else {
            exNode->AtkResNode.SetPositionFloat(iconNode->AtkResNode.X - 28 + iconPaddingRight,
                iconNode->AtkResNode.Y);
        }

        var jobId = wrapper.NamePlateInfo.GetJobID();

        exNode->LoadIconTexture((int)iconGroup.GetJobIcon(jobId), 0);

        info.State = NameplateUpdater.PlateState.Player;
    }

    private unsafe void PrepareNodeLargeCenter(NameplateUpdater.PlateInfo info, NamePlateObjectWrapper wrapper)
    {
        var iconGroup = GetIconGroup(wrapper.NamePlateInfo);

        const short yAdjust = -10;
        var iconPaddingBottom = iconGroup.Padding.Bottom;

        var exNode = info.ExIconNode;
        exNode->AtkResNode.OriginX = ExIconWidth / 2f;
        exNode->AtkResNode.OriginY = ExIconHeight - iconPaddingBottom;

        var scale = iconGroup.Scale;
        scale *= 2.1f;
        exNode->AtkResNode.SetScale(scale, scale);
        exNode->AtkResNode.SetPositionFloat(144 - ExIconWidth / 2f, 112 - ExIconHeight + yAdjust + iconPaddingBottom);

        var jobId = wrapper.NamePlateInfo.GetJobID();
        exNode->LoadIconTexture((int)iconGroup.GetJobIcon(jobId), 0);

        info.State = NameplateUpdater.PlateState.Player;
    }

    public unsafe void PrepareNodeLargeLeft(NameplateUpdater.PlateInfo info, NamePlateObjectWrapper wrapper)
    {
        Service.Log.Info($"TextW: {info.NamePlateObject->TextW}");
        if (info.NamePlateObject->TextW == 0) {
            PrepareNodeLargeCenter(info, wrapper);
            return;
        }

        var iconGroup = GetIconGroup(wrapper.NamePlateInfo);

        const short exWidth = 32;
        const short exHeight = 32;
        const short yAdjust = -18;
        const short xAdjust = 2;
        var iconPaddingRight = iconGroup.Padding.Right;

        var exNode = info.ExIconNode;
        exNode->AtkResNode.OriginX = exWidth - iconPaddingRight;
        exNode->AtkResNode.OriginY = exHeight / 2f;

        var scale = iconGroup.Scale;
        scale *= 1.55f;
        exNode->AtkResNode.SetScale(scale, scale);
        exNode->AtkResNode.SetPositionFloat(144 - exWidth + iconPaddingRight + xAdjust, 112 - exHeight + yAdjust);

        var jobId = wrapper.NamePlateInfo.GetJobID();
        exNode->LoadIconTexture((int)iconGroup.GetJobIcon(jobId), 0);

        info.State = NameplateUpdater.PlateState.Player;
    }

    public void NameplateDataForPC(NamePlateObjectWrapper npObject,
        ref bool isPrefixTitle,
        ref bool displayTitle,
        ref IntPtr title,
        ref IntPtr name,
        ref IntPtr fcName,
        ref IntPtr prefix,
        ref uint iconID,
        out bool usedTextIcon)
    {
        usedTextIcon = false;
        //name = SeStringUtils.SeStringToPtr(SeStringUtils.Text("Plugin Enjoyer"));
        var uid = npObject.NamePlateInfo.ObjectID;
        var mode = GetModeForNameplate(npObject);

        if (_configuration.HideLocalPlayerNameplate && uid == Service.ClientState.LocalPlayer?.ObjectId) {
            switch (mode) {
                case NameplateMode.Default:
                case NameplateMode.Hide:
                case NameplateMode.SmallJobIcon:
                case NameplateMode.SmallJobIconAndRole:
                case NameplateMode.BigJobIcon:
                case NameplateMode.BigJobIconAndPartySlot:
                    name = SeStringUtils.emptyPtr;
                    fcName = SeStringUtils.emptyPtr;
                    displayTitle = false;
                    iconID = 0;

                    return;

                case NameplateMode.RoleLetters:
                    if (!_configuration.TestingMode && !npObject.NamePlateInfo.IsPartyMember()) {
                        name = SeStringUtils.emptyPtr;
                        fcName = SeStringUtils.emptyPtr;
                        displayTitle = false;
                        iconID = 0;

                        return;
                    }

                    break;
            }
        }

        var playerCharacter = Service.ObjectTable.SearchById(uid) as PlayerCharacter;

        if (playerCharacter == null) {
            return;
        }

        var hasRole = _roleTracker.TryGetAssignedRole(playerCharacter.Name.TextValue, playerCharacter.HomeWorld.Id,
            out var roleId);

        switch (mode) {
            case NameplateMode.Default:
            case NameplateMode.Hide:
                return;
            case NameplateMode.SmallJobIcon:
            {
                // var icon = StatusUtils.OnlineStatusToBitmapIcon(npObject.NamePlateInfo.GetOnlineStatus());
                // if (icon is not BitmapFontIcon.None) {
                //     var nameString = new SeString()
                //         .Append(new IconPayload(icon))
                //         .Append(SeStringUtils.SeStringFromPtr(name));
                //     name = SeStringUtils.SeStringToPtr(nameString);
                //     usedTextIcon = true;
                // }

                prefix = SeStringUtils.emptyPtr;
                iconID = StatusUtils.OnlineStatusToIconId(npObject.NamePlateInfo.GetOnlineStatus());
                // iconID = GetClassIcon(npObject.NamePlateInfo);
                break;
            }
            case NameplateMode.SmallJobIconAndRole:
            {
                if (hasRole) {
                    var nameString = new SeString()
                        .Append(_stylesheet.GetRolePlate(roleId))
                        .Append(" ")
                        .Append(SeStringUtils.SeStringFromPtr(name));
                    name = SeStringUtils.SeStringToPtr(nameString);
                    usedTextIcon = true;
                }
                // else {
                //     var icon = StatusUtils.OnlineStatusToBitmapIcon(npObject.NamePlateInfo.GetOnlineStatus());
                //     if (icon is not BitmapFontIcon.None) {
                //         var nameString = new SeString()
                //             .Append(new IconPayload(icon))
                //             .Append(SeStringUtils.SeStringFromPtr(name));
                //         name = SeStringUtils.SeStringToPtr(nameString);
                //         usedTextIcon = true;
                //     }
                // }

                prefix = SeStringUtils.emptyPtr;
                iconID = StatusUtils.OnlineStatusToIconId(npObject.NamePlateInfo.GetOnlineStatus());
                // iconID = GetClassIcon(npObject.NamePlateInfo);
                break;
            }
            case NameplateMode.BigJobIcon:
            {
                // var icon = StatusUtils.OnlineStatusToBitmapIcon(npObject.NamePlateInfo.GetOnlineStatus());
                // if (icon is not BitmapFontIcon.None) {
                //     var nameString = new SeString()
                //         .Append("  ")
                //         .Append(new IconPayload(icon));
                //     name = SeStringUtils.SeStringToPtr(nameString);
                //     usedTextIcon = true;
                // }
                // else {
                    name = SeStringUtils.emptyPtr;
                // }

                displayTitle = false;
                fcName = SeStringUtils.emptyPtr;
                prefix = SeStringUtils.emptyPtr;
                // iconID = GetClassIcon(npObject.NamePlateInfo);
                iconID = 0;
                break;
            }
            case NameplateMode.BigJobIconAndPartySlot:
            {
                fcName = SeStringUtils.emptyPtr;
                displayTitle = false;
                var partySlot = _partyListHudView.GetPartySlotIndex(npObject.NamePlateInfo.ObjectID) +
                                1;

                if (partySlot != null) {
                    var genericRole = ((Job)npObject.NamePlateInfo.GetJobID()).GetRole();
                    var str = _stylesheet.GetPartySlotNumber(partySlot.Value, genericRole);
                    // str.Payloads.Insert(0, new TextPayload("   "));
                    str.Payloads.Insert(0, new TextPayload("　"));
                    name = SeStringUtils.SeStringToPtr(str);
                    iconID = 0;
                }
                else {
                    name = SeStringUtils.emptyPtr;
                    iconID = 0;
                }

                prefix = SeStringUtils.emptyPtr;

                break;
            }
            case NameplateMode.RoleLetters:
            {
                if (hasRole) {
                    name = SeStringUtils.SeStringToPtr(_stylesheet.GetRolePlate(roleId));
                }
                else {
                    var genericRole = ((Job)npObject.NamePlateInfo.GetJobID()).GetRole();
                    name = SeStringUtils.SeStringToPtr(_stylesheet.GetGenericRolePlate(genericRole));
                }

                iconID = 0;
                prefix = SeStringUtils.emptyPtr;
                fcName = SeStringUtils.emptyPtr;
                displayTitle = false;
                break;
            }
        }
    }

    public uint GetClassIcon(NamePlateInfoWrapper info)
    {
        var genericRole = JobExtensions.GetRole((Job)info.GetJobID());
        var iconSet = _stylesheet.GetGenericRoleIconGroupId(genericRole);

        return IconRegistrar.Get(iconSet).GetJobIcon(info.GetJobID());
    }

    public IconGroup GetIconGroup(NamePlateInfoWrapper info)
    {
        var genericRole = JobExtensions.GetRole((Job)info.GetJobID());
        var iconSet = _stylesheet.GetGenericRoleIconGroupId(genericRole);

        return IconRegistrar.Get(iconSet);
    }

    private NameplateMode GetModeForNameplate(NamePlateObjectWrapper npObject)
    {
        var uid = npObject.NamePlateInfo.ObjectID;

        if (_configuration.TestingMode || npObject.NamePlateInfo.IsPartyMember() ||
            uid == Service.ClientState.LocalPlayer?.ObjectId) {
            return PartyMode;
        }

        return OthersMode;
    }
}