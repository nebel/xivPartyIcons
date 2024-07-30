using static FFXIVClientStructs.FFXIV.Component.Excel.IExcelRowWrapper.Delegates;

namespace PartyIcons.Entities;

public enum GenericRole : uint
{
    Tank = 0,
    Melee = 1,
    Ranged = 2,
    Healer = 3,
    Crafter = 4,
    Gatherer = 5,
    Caster = 6,
}
