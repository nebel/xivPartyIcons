using Dalamud.Game.Text.SeStringHandling;

namespace PartyIcons.Dalamud;

public class NamePlateSimplePartBuilder(NamePlateStringField field)
{
    public (SeString, SeString)? TextColor { get; set; }
    public SeString? Text { get; set; }

    internal unsafe void Apply(NamePlateUpdateHandler handler)
    {
        if (handler.GetStringValueAsPointer(field) == NamePlateUpdateHandler.EmptyStringPointer)
            return;

        if (TextColor is { Item1: var left, Item2: var right }) {
            var sb = new SeStringBuilder();
            sb.Append(left);
            sb.Append(Text ?? handler.GetStringValueAsSeString(field));
            sb.Append(right);
            handler.SetField(field, sb.Build());
        }
        else if (Text is not null) {
            handler.SetField(field, Text);
        }
    }
}