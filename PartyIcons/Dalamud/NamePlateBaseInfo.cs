using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace PartyIcons.Dalamud;

public class NamePlateBaseInfo
{
    public SeString Name { get; }
    public SeString FreeCompanyTag { get; }
    public SeString Title { get; }
    public SeString LevelText { get; }
    public SeString DisplayTitle { get; }
    public int Flags { get; }
    public bool IsDirty { get; }
    public bool IsPrefixTitle { get; }

    public unsafe NamePlateBaseInfo(RaptureAtkModule.NamePlateInfo* info)
    {
        Name = SeString.Parse(info->Name);
        FreeCompanyTag = SeString.Parse(info->FcName);
        Title = SeString.Parse(info->Title);
        DisplayTitle = SeString.Parse(info->DisplayTitle);
        LevelText = SeString.Parse(info->LevelText);
        Flags = info->Flags;
        IsDirty = info->IsDirty;
        IsPrefixTitle = ((info->Flags >> (8 * 3)) & 0xFF) == 1;
    }
}