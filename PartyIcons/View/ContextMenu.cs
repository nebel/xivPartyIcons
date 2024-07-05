using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text;
using PartyIcons.Configuration;
using PartyIcons.Entities;
using PartyIcons.Runtime;
using PartyIcons.Stylesheet;

namespace PartyIcons.View;

public sealed class ContextMenu : IDisposable
{
    private readonly RoleTracker _roleTracker;
    private readonly Settings _configuration;
    private readonly PlayerStylesheet _stylesheet;

    private record CharacterInfo(string Name, uint World, RoleId? AssignedRole, RoleId? SuggestedRole);

    public ContextMenu(RoleTracker roleTracker, Settings configuration, PlayerStylesheet stylesheet)
    {
        _roleTracker = roleTracker;
        _configuration = configuration;
        _stylesheet = stylesheet;

        Service.ContextMenu.OnMenuOpened += OnMenuOpened;
    }

    public void Dispose()
    {
        Service.ContextMenu.OnMenuOpened -= OnMenuOpened;
    }

    private CharacterInfo? GetCharacterInfo(IMenuOpenedArgs args)
    {
        if (args is { MenuType: ContextMenuType.Default, Target: MenuTargetDefault menuTarget }) {
            if (menuTarget.TargetCharacter is { Name: {} tcName, HomeWorld.Id: var tcWorld }) {
                return new CharacterInfo(
                    tcName,
                    tcWorld,
                    _roleTracker.TryGetAssignedRole(tcName, tcWorld, out var assigned) ? assigned : null,
                    _roleTracker.TryGetSuggestedRole(tcName, tcWorld, out var suggested) ? suggested : null
                );
            }

            if (menuTarget.TargetObject is IPlayerCharacter { Name.TextValue: {} pcName, HomeWorld.Id: var pcWorld }) {
                return new CharacterInfo(
                    pcName,
                    pcWorld,
                    _roleTracker.TryGetAssignedRole(pcName, pcWorld, out var assigned) ? assigned : null,
                    _roleTracker.TryGetSuggestedRole(pcName, pcWorld, out var suggested) ? suggested : null
                );
            }
        }

        return null;
    }

    private void OnMenuOpened(IMenuOpenedArgs args)
    {
        if (!_configuration.UseContextMenu || GetCharacterInfo(args) is not { } characterInfo) {
            return;
        }

        if (_configuration.UseContextMenuSubmenu) {
            var menuName = "Set Role";
            if (characterInfo.AssignedRole is { } assignedRole) {
                menuName = $"Change Role ({_stylesheet.GetRoleName(assignedRole)})";
            }

            args.AddMenuItem(new MenuItem
            {
                Name = menuName,
                Prefix = SeIconChar.BoxedLetterP,
                PrefixColor = 37,
                Priority = 10,
                IsSubmenu = true,
                OnClicked = subArgs =>
                {
                    var items = CreateMenuItems(characterInfo);
                    if (items.Count == 0) {
                        items.Add(new MenuItem
                        {
                            Name = "No actions available",
                            IsEnabled = false
                        });
                    }

                    subArgs.OpenSubmenu(items);
                }
            });
        }
        else {
            foreach (var item in CreateMenuItems(characterInfo)) {
                args.AddMenuItem(item);
            }
        }
    }

    private List<MenuItem> CreateMenuItems(CharacterInfo characterInfo)
    {
        var list = new List<MenuItem>();
        AddRoleSuggestion(list, characterInfo);
        AddRoleSwap(list, characterInfo);
        AddRoleAssignments(list, characterInfo);
        return list;
    }

    private void AddRoleSuggestion(ICollection<MenuItem> list, CharacterInfo characterInfo)
    {
        if (characterInfo.SuggestedRole is { } role) {
            list.Add(new MenuItem
            {
                Name = $"Assign {_stylesheet.GetRoleName(role)} (suggested)",
                Prefix = SeIconChar.ArrowRight,
                PrefixColor = 31,
                Priority = 100,
                OnClicked = _ => { AssignRole(characterInfo.Name, characterInfo.World, role); }
            });
        }
    }

    private void AddRoleSwap(ICollection<MenuItem> list, CharacterInfo characterInfo)
    {
        if (characterInfo.AssignedRole is { } role) {
            var swappedRole = RoleIdUtils.Counterpart(role);
            list.Add(new MenuItem
            {
                Name = $"Swap to {_stylesheet.GetRoleName(swappedRole)}",
                Prefix = SeIconChar.ArrowRight,
                PrefixColor = 3,
                Priority = 101,
                OnClicked = _ => { AssignRole(characterInfo.Name, characterInfo.World, swappedRole); }
            });
        }
    }

    private void AddRoleAssignments(ICollection<MenuItem> list, CharacterInfo characterInfo)
    {
        foreach (var role in Enum.GetValues<RoleId>()) {
            if (role == RoleId.Undefined) {
                continue;
            }

            var roleName = _stylesheet.GetRoleName(role);

            list.Add(new MenuItem
            {
                Name = $"Assign {roleName}",
                Prefix = (SeIconChar)PlayerStylesheet.BoxedCharacter(roleName[0]),
                PrefixColor = PlayerStylesheet.GetRoleColor(role),
                Priority = 101 + (int)role,
                OnClicked = _ => { AssignRole(characterInfo.Name, characterInfo.World, role); }
            });
        }
    }

    private void AssignRole(string name, uint world, RoleId role)
    {
        _roleTracker.OccupyRole(name, world, role);
        _roleTracker.CalculateUnassignedPartyRoles();
    }
}