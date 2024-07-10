namespace PartyIcons.Dalamud;

internal class NamePlatePartBuilder
{
    internal NamePlateSimplePartBuilder? NameBuilder;
    internal NamePlateQuotedPartBuilder? TitleBuilder;
    internal NamePlateQuotedPartBuilder? FreeCompanyTagBuilder;

    public NamePlatePartBuilder(NamePlateUpdateContext context)
    {
        context.hasBuilders = true;
    }

    internal NamePlateSimplePartBuilder Name => NameBuilder ??= new NamePlateSimplePartBuilder(NamePlateStringField.Name);

    internal NamePlateQuotedPartBuilder Title => TitleBuilder ??= new NamePlateQuotedPartBuilder(NamePlateStringField.Title, false);

    internal NamePlateQuotedPartBuilder FreeCompanyTag => FreeCompanyTagBuilder ??= new NamePlateQuotedPartBuilder(NamePlateStringField.FreeCompanyTag, true);
}