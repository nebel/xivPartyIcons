using System.Collections.Generic;

namespace PartyIcons.View;

public static class IconRegistrar
{
    private static readonly Dictionary<IconGroupId, IconGroup> Icons = new();

    static IconRegistrar()
    {
        // Embossed gold/silver [game default]
        Add(IconGroupId.Embossed, 1, new Padding(2.5f), [
            62001, 62002, 62003, 62004, 62005, 62006, 62007, 62008, 62009, 62010,
            62011, 62012, 62013, 62014, 62015, 62016, 62017, 62018, 62019, 62020,
            62021, 62022, 62023, 62024, 62025, 62026, 62027, 62028, 62029, 62030,
            62031, 62032, 62033, 62034, 62035, 62036, 62037, 62038, 62039, 62040,
            62041, 62042
        ]);

        // Embossed gold/silver with gold frame and role-colored backdrop
        Add(IconGroupId.EmbossedFramed, 1, new Padding(0.5f), [
            62101, 62102, 62103, 62104, 62105, 62106, 62107, 62108, 62109, 62110,
            62111, 62112, 62113, 62114, 62115, 62116, 62117, 62118, 62119, 62120,
            62121, 62122, 62123, 62124, 62125, 62126, 62127, 62128, 62129, 62130,
            62131, 62132, 62133, 62134, 62135, 62136, 62137, 62138, 62139, 62140,
            62141, 62142
        ]);

        // Embossed gold/silver with gold frame and role-colored backdrop, smaller
        Add(IconGroupId.EmbossedFramedSmall, 1, new Padding(5f), [
            62226, 62227, 62228, 62229, 62230, 62231, 62232, 62233, 62234, 62235,
            62236, 62237, 62238, 62239, 62240, 62241, 62242, 62243, 62244, 62245,
            62246, 62247, 62248, 62249, 62250, 62251, 62252, 62253, 62254, 62255,
            62256, 62257, 62258, 62259, 62260, 62261, 62262, 62263, 62264, 62265,
            62266, 62267
        ]);

        // Embossed blue/green/red role icons with gold frame and role-colored backdrop
        Add(IconGroupId.EmbossedFramedRole, 1, new Padding(0), [
            62581, 62584, 62581, 62584, 62586, 62582, 62502, 62502, 62503, 62504,
            62505, 62506, 62507, 62508, 62509, 62510, 62511, 62512, 62581, 62584,
            62581, 62584, 62586, 62582, 62587, 62587, 62587, 62582, 62584, 62584,
            62586, 62581, 62582, 62584, 62587, 62587, 62581, 62586, 62584, 62582,
            62584, 62587
        ]);

        // Glowing gold/silver
        Add(IconGroupId.Glowing, 1, new Padding(2.5f), [
            62301, 62302, 62303, 62304, 62305, 62306, 62307, 62310, 62311, 62312,
            62313, 62314, 62315, 62316, 62317, 62318, 62319, 62320, 62401, 62402,
            62403, 62404, 62405, 62406, 62407, 62308, 62408, 62409, 62309, 62410,
            62411, 62412, 62413, 62414, 62415, 62416, 62417, 62418, 62419, 62420,
            62421, 62422
        ]);

        // Colored gradients with thin black border
        const float gradientScale = 1.45f;
        var gradientPadding = new Padding(6.5f);
        Add(IconGroupId.GradientGrey, gradientScale, gradientPadding, [
            91022, 91023, 91024, 91025, 91026, 91028, 91029, 91031, 91032, 91033,
            91034, 91035, 91036, 91037, 91038, 91039, 91040, 91041, 91079, 91080,
            91081, 91082, 91083, 91084, 91085, 91030, 91086, 91087, 91121, 91122,
            91125, 91123, 91124, 91127, 91128, 91129, 91130, 91131, 91132, 91133,
            91185, 91186
        ]);
        Add(IconGroupId.GradientBlack, gradientScale, gradientPadding, [
            91522, 91523, 91524, 91525, 91526, 91528, 91529, 91531, 91532, 91533,
            91534, 91535, 91536, 91537, 91538, 91539, 91540, 91541, 91579, 91580,
            91581, 91582, 91583, 91584, 91585, 91530, 91586, 91587, 91621, 91622,
            91625, 91623, 91624, 91627, 91628, 91629, 91630, 91631, 91632, 91633,
            91685, 91686
        ]);
        Add(IconGroupId.GradientYellow, gradientScale, gradientPadding, [
            92022, 92023, 92024, 92025, 92026, 92028, 92029, 92031, 92032, 92033,
            92034, 92035, 92036, 92037, 92038, 92039, 92040, 92041, 92079, 92080,
            92081, 92082, 92083, 92084, 92085, 92030, 92086, 92087, 92121, 92122,
            92125, 92123, 92124, 92127, 92128, 92129, 92130, 92131, 92132, 92133,
            92185, 92186
        ]);
        Add(IconGroupId.GradientOrange, gradientScale, gradientPadding, [
            92522, 92523, 92524, 92525, 92526, 92528, 92529, 92531, 92532, 92533,
            92534, 92535, 92536, 92537, 92538, 92539, 92540, 92541, 92579, 92580,
            92581, 92582, 92583, 92584, 92585, 92530, 92586, 92587, 92621, 92622,
            92625, 92623, 92624, 92627, 92628, 92629, 92630, 92631, 92632, 92633,
            92685, 92686
        ]);
        Add(IconGroupId.GradientRed, gradientScale, gradientPadding, [
            93022, 93023, 93024, 93025, 93026, 93028, 93029, 93031, 93032, 93033,
            93034, 93035, 93036, 93037, 93038, 93039, 93040, 93041, 93079, 93080,
            93081, 93082, 93083, 93084, 93085, 93030, 93086, 93087, 93121, 93122,
            93125, 93123, 93124, 93127, 93128, 93129, 93130, 93131, 93132, 93133,
            93185, 93186
        ]);
        Add(IconGroupId.GradientPurple, gradientScale, gradientPadding, [
            93522, 93523, 93524, 93525, 93526, 93528, 93529, 93531, 93532, 93533,
            93534, 93535, 93536, 93537, 93538, 93539, 93540, 93541, 93579, 93580,
            93581, 93582, 93583, 93584, 93585, 93530, 93586, 93587, 93621, 93622,
            93625, 93623, 93624, 93627, 93628, 93629, 93630, 93631, 93632, 93633,
            93685, 93686
        ]);
        Add(IconGroupId.GradientBlue, gradientScale, gradientPadding, [
            94022, 94023, 94024, 94025, 94026, 94028, 94029, 94031, 94032, 94033,
            94034, 94035, 94036, 94037, 94038, 94039, 94040, 94041, 94079, 94080,
            94081, 94082, 94083, 94084, 94085, 94030, 94086, 94087, 94121, 94122,
            94125, 94123, 94124, 94127, 94128, 94129, 94130, 94131, 94132, 94133,
            94185, 94186
        ]);
        Add(IconGroupId.GradientGreen, gradientScale, gradientPadding, [
            94522, 94523, 94524, 94525, 94526, 94528, 94529, 94531, 94532, 94533,
            94534, 94535, 94536, 94537, 94538, 94539, 94540, 94541, 94579, 94580,
            94581, 94582, 94583, 94584, 94585, 94530, 94586, 94587, 94621, 94622,
            94625, 94623, 94624, 94627, 94628, 94629, 94630, 94631, 94632, 94633,
            94685, 94686
        ]);
    }

    private static void Add(IconGroupId id, float scale, Padding padding, uint[] icons)
    {
        Icons[id] = new IconGroup(id, scale, padding, icons);
    }

    public static IconGroup Get(IconGroupId groupId)
    {
        return Icons[groupId];
    }

    public static readonly IconGroup Status = new(IconGroupId.Status, 1, new Padding(0), []);
}

public record IconGroup
{
    public readonly IconGroupId Id;
    public readonly float Scale = 1f;
    public readonly Padding Padding = new(0);
    private readonly uint[] _icons;

    public IconGroup(IconGroupId id, float scale, Padding padding, uint[] icons)
    {
        Id = id;
        Scale = scale;
        Padding = padding;
        _icons = icons;
    }

    public uint GetJobIcon(uint jobId) => _icons[jobId - 1];
}

public struct Padding
{
    public readonly float Top;
    public readonly float Right;
    public readonly float Bottom;
    public readonly float Left;

    public Padding(float size)
    {
        Top = Right = Bottom = Left = size;
    }

    public Padding(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }
}

public enum IconGroupId
{
    Embossed,
    EmbossedFramed,
    EmbossedFramedSmall,
    EmbossedFramedRole,
    Glowing,
    GradientGrey,
    GradientBlack,
    GradientYellow,
    GradientOrange,
    GradientRed,
    GradientPurple,
    GradientBlue,
    GradientGreen,
    Status
}