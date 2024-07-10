using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace PartyIcons.Dalamud;

/// <summary>
/// Provides a read-only view of the backing data for a nameplate. Modifications to <see cref="NamePlateUpdateHandler"/> fields do not affect this data.
/// </summary>
public unsafe class NamePlateBaseInfo(RaptureAtkModule.NamePlateInfo* info)
{
    private SeString? name;
    private SeString? freeCompanyTag;
    private SeString? title;
    private SeString? displayTitle;
    private SeString? levelText;

    public SeString Name => name ??= SeString.Parse(info->Name);
    public SeString FreeCompanyTag => freeCompanyTag ??= SeString.Parse(info->FcName);
    public SeString Title => title ??= SeString.Parse(info->Title);
    public SeString DisplayTitle => displayTitle ??= SeString.Parse(info->DisplayTitle);
    public SeString LevelText => levelText ??= SeString.Parse(info->LevelText);
    public int Flags = info->Flags;
    public bool IsDirty => info->IsDirty;
    public bool IsPrefixTitle = ((info->Flags >> (8 * 3)) & 0xFF) == 1;
}