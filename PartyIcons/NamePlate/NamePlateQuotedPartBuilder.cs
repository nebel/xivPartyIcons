
using Dalamud.Game.Text.SeStringHandling;

namespace PartyIcons.Dalamud;

public class NamePlateQuotedPartBuilder(NamePlateStringField field, bool isFreeCompany)
{
    public SeString? LeftQuote { get; set; }
    public SeString? RightQuote { get; set; }
    public (SeString, SeString)? TextColor { get; set; }
    public SeString? Text { get; set; }

    internal unsafe void Apply(NamePlateUpdateHandler handler)
    {
        if (handler.GetStringValueAsPointer(field) == NamePlateUpdateHandler.EmptyStringPointer)
            return;

        var sb = new SeStringBuilder();
        if (LeftQuote is not null) {
            sb.Append(LeftQuote);
        }
        else {
            sb.Append(isFreeCompany ? " «" : "《");
        }

        if (TextColor is { Item1: var left, Item2: var right }) {
            sb.Append(left);
            sb.Append(Text ?? handler.GetStringValueAsSeString(field));
            sb.Append(right);
        }
        else {
            sb.Append(Text ?? handler.GetStringValueAsSeString(field));
        }

        if (RightQuote is not null) {
            sb.Append(RightQuote);
        }
        else {
            sb.Append(isFreeCompany ? "»" : "》");
        }

        handler.SetField(field, sb.Build());
    }
}
