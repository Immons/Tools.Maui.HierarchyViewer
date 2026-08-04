using static Immons.Tools.Maui.Inspector.Features.Properties.SectionBuilder;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Sections;

/// <summary>Text content, fonts, alignment and input-specific rows.</summary>
internal sealed class TextSectionBuilder : IPropertySectionBuilder
{
    public IEnumerable<PropertySection> Build(InspectionContext context)
    {
        var el = context.Element;
        var s = New("Text");

        if (el is IText)
            AddEditable(s, el, "Text");

        if (el is ITextStyle)
        {
            AddEditable(s, el, "FontFamily");
            AddEditable(s, el, "FontSize");
            AddEditable(s, el, "FontAttributes");
            AddEditable(s, el, "TextColor");
            AddEditable(s, el, "CharacterSpacing");
        }

        if (el is ITextAlignment)
        {
            AddEditable(s, el, "HorizontalTextAlignment");
            AddEditable(s, el, "VerticalTextAlignment");
        }

        switch (el)
        {
            case Label label:
                AddEditable(s, el, "LineBreakMode");
                AddEditable(s, el, "MaxLines");
                if (label.LineHeight >= 0)
                    AddEditable(s, el, "LineHeight");
                AddEditable(s, el, "TextDecorations");
                AddEditable(s, el, "TextTransform");
                break;
            case InputView input:
                AddEditable(s, el, "Placeholder");
                AddEditable(s, el, "PlaceholderColor");
                if (input.MaxLength >= 0 && input.MaxLength != int.MaxValue)
                    AddEditable(s, el, "MaxLength");
                s.Rows.Add(new PropertyRow("Keyboard", KeyboardCatalog.NameOf(input.Keyboard), null,
                    KeyboardCatalog.CreateEditor(input)));
                break;
        }

        // Last row so the toggled span sections appear right below it.
        if (el is Label label2)
            s.Rows.Add(FormattedTextRow(label2));

        yield return s;
    }

    static PropertyRow FormattedTextRow(Label label)
    {
        if (label.FormattedText is { } ft)
            return new PropertyRow("FormattedText", $"{ft.Spans.Count} span(s)", TogglesGroup: "spans");

        return new PropertyRow("FormattedText", "＋ Create (converts Text into a span)", Action: () =>
        {
            var formatted = new FormattedString();
            formatted.Spans.Add(new Span { Text = label.Text ?? "New span" });
            label.FormattedText = formatted;
        });
    }
}
