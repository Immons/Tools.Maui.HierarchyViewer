using static Immons.Tools.Maui.Inspector.Shared.ValueFormatter;
using static Immons.Tools.Maui.Inspector.Features.Properties.SectionBuilder;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Sections;

/// <summary>Current XAML Style (resolved to its resource key when possible), its setters,
/// and a picker applying any other style from reachable resources with a matching TargetType.</summary>
internal sealed class StyleSectionBuilder : IPropertySectionBuilder
{
    public IEnumerable<PropertySection> Build(InspectionContext context)
    {
        var el = context.Element;
        var s = New("Style");
        var current = el.Style;

        // Implicit styles are keyed by the full type name — show them as "(implicit X)".
        var available = StyleCatalog.CollectStyles(el)
            .Select(t => (Name: t.Key == t.Style.TargetType?.FullName
                ? $"(implicit {t.Style.TargetType.Name})"
                : t.Key, t.Style))
            .ToList();

        var currentName = current == null
            ? "(none)"
            : available.FirstOrDefault(t => ReferenceEquals(t.Style, current)).Name ?? "(inline / unnamed)";

        var choices = new List<string> { "(none)" };
        choices.AddRange(available.Select(t => t.Name));

        var editor = new PropertyEditor(EditorKind.Enum, choices, name =>
        {
            if (name == "(none)")
            {
                el.Style = null;
                return true;
            }
            var match = available.FirstOrDefault(t => t.Name == name);
            if (match.Style == null)
                return false;
            el.Style = match.Style;
            ClearLocalValuesShadowing(el, match.Style);
            return true;
        })
        {
            XamlTarget = el,
            XamlAttribute = "Style",
            XamlValue = name => name == "(none)" ? XamlChangeLog.RemoveMarker
                : name.StartsWith('(') ? null
                : $"{{StaticResource {name}}}",
        };
        s.Rows.Add(new PropertyRow("Style", currentName, null, editor));

        if (current != null)
        {
            if (current.BasedOn is { } basedOn)
                Add(s, "BasedOn", available.FirstOrDefault(t => ReferenceEquals(t.Style, basedOn)).Name
                                  ?? basedOn.TargetType?.Name ?? "(style)");
            foreach (var setter in current.Setters)
                Add(s, $"  {setter.Property?.PropertyName ?? "?"}", FormatValue(setter.Value),
                    setter.Value switch { Color c => c, SolidColorBrush b => b.Color, _ => null });
        }

        if (el.StyleClass is { Count: > 0 } styleClass)
            Add(s, "StyleClass", string.Join(", ", styleClass));

        yield return s;
    }

    /// <summary>Locally-set values outrank style setters — clear them so the style takes effect.</summary>
    static void ClearLocalValuesShadowing(VisualElement el, Style style)
    {
        for (var st = style; st != null; st = st.BasedOn)
        {
            foreach (var setter in st.Setters)
            {
                if (setter.Property is { } bp)
                    el.ClearValue(bp);
            }
        }
    }
}
