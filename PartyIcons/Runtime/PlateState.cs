using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyIcons.Runtime;

public unsafe class PlateState
{
    public AddonNamePlate.NamePlateObject* NamePlateObject;
    public AtkCollisionNode* CollisionNode;
    public AtkResNode* ResNode;
    public AtkTextNode* NameTextNode;
    public AtkImageNode* IconNode;
    public AtkImageNode* ExIconNode;
    public AtkImageNode* SubIconNode;

    public bool IsModified = false;
    public bool IsGlobalScaleModified = false;

    public float AdditionalCollisionScale = 1f;
    public bool IsCollisionFixed = true;

    public bool IsIconBlank = false;
    public bool UseExIcon;
    public bool UseSubIcon;

    public override string ToString()
    {
        return $"{nameof(NamePlateObject)}: 0x{(nint)NamePlateObject:X}, " +
               // $"{nameof(ComponentNode)}: 0x{(nint)ComponentNode:X}, " +
               // $"{nameof(ResNode)}: 0x{(nint)ResNode:X}, " +
               // $"{nameof(NameTextNode)}: 0x{(nint)NameTextNode:X}, " +
               // $"{nameof(NameCollisionNode)}: 0x{(nint)NameCollisionNode:X}, " +
               // $"{nameof(IconNode)}: 0x{(nint)IconNode:X}, " +
               $"{nameof(ExIconNode)}: 0x{(nint)ExIconNode:X} ";
    }
}