using System.Globalization;
using static Immons.Tools.Maui.Inspector.Shared.ValueFormatter;
using static Immons.Tools.Maui.Inspector.Features.Properties.SectionBuilder;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Sections;

/// <summary>SemanticProperties (editable, synced to XAML) plus a WCAG contrast check for text.</summary>
internal sealed class AccessibilitySectionBuilder : IPropertySectionBuilder
{
    public IEnumerable<PropertySection> Build(InspectionContext context)
    {
        var el = context.Element;
        var s = New("Accessibility");

        s.Rows.Add(new PropertyRow("Description", SemanticProperties.GetDescription(el) ?? "", null,
            new PropertyEditor(EditorKind.Text, null, text =>
            {
                el.SetValue(SemanticProperties.DescriptionProperty, text.Length == 0 ? null : text);
                return true;
            })
            { XamlTarget = el, XamlAttribute = "SemanticProperties.Description" }));

        s.Rows.Add(new PropertyRow("Hint", SemanticProperties.GetHint(el) ?? "", null,
            new PropertyEditor(EditorKind.Text, null, text =>
            {
                el.SetValue(SemanticProperties.HintProperty, text.Length == 0 ? null : text);
                return true;
            })
            { XamlTarget = el, XamlAttribute = "SemanticProperties.Hint" }));

        s.Rows.Add(new PropertyRow("HeadingLevel", SemanticProperties.GetHeadingLevel(el).ToString(), null,
            new PropertyEditor(EditorKind.Enum, Enum.GetNames(typeof(SemanticHeadingLevel)), text =>
            {
                if (!Enum.TryParse<SemanticHeadingLevel>(text, true, out var level))
                    return false;
                el.SetValue(SemanticProperties.HeadingLevelProperty, level);
                return true;
            })
            { XamlTarget = el, XamlAttribute = "SemanticProperties.HeadingLevel" }));

        if (el is ITextStyle { TextColor: { } textColor })
        {
            var background = WcagContrast.FindEffectiveBackground(el, out var assumed);
            var ratio = WcagContrast.Ratio(textColor, background);
            Add(s, "Contrast (WCAG)",
                $"{ratio.ToString("F2", CultureInfo.InvariantCulture)}:1 — {WcagContrast.Rating(ratio)}"
                + $"  vs {Format(background)}{(assumed ? " (assumed)" : "")}",
                background);
        }

        yield return s;
    }
}
