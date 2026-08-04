using static Immons.Tools.Maui.Inspector.Features.Properties.SectionBuilder;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Sections;

/// <summary>Per-span sections of a Label's FormattedText, plus the add-span action.</summary>
internal sealed class SpanSectionBuilder : IPropertySectionBuilder
{
    public IEnumerable<PropertySection> Build(InspectionContext context)
    {
        if (context.Element is not Label { FormattedText: { } ft })
            yield break;

        for (var i = 0; i < ft.Spans.Count; i++)
            yield return BuildSpan(ft, ft.Spans[i], i);

        var actions = New("Spans", group: "spans");
        actions.Rows.Add(new PropertyRow("", "＋ Add span",
            Action: () => ft.Spans.Add(new Span { Text = "New span" })));
        yield return actions;
    }

    static PropertySection BuildSpan(FormattedString formatted, Span span, int index)
    {
        var s = New($"Span {index + 1}", group: "spans");
        AddEditable(s, span, "Text");
        AddEditable(s, span, "FontFamily");
        AddEditable(s, span, "FontSize");
        AddEditable(s, span, "FontAttributes");
        AddEditable(s, span, "TextColor");
        AddEditable(s, span, "BackgroundColor");
        AddEditable(s, span, "CharacterSpacing");
        AddEditable(s, span, "TextDecorations");
        AddEditable(s, span, "LineHeight");
        s.Rows.Add(new PropertyRow("", "✕ Remove span",
            Action: () => formatted.Spans.Remove(span)));
        return s;
    }
}
