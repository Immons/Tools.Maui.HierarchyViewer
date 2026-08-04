using static Immons.Tools.Maui.Inspector.Features.Properties.SectionBuilder;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Sections;

/// <summary>Every public simple-typed property of the element, editable where a setter exists.</summary>
internal sealed class AllPropertiesSectionBuilder : IPropertySectionBuilder
{
    static readonly HashSet<string> Skip =
    [
        "BindingContext", "StyleId", "AutomationId", "ClassId", "Id",
    ];

    public IEnumerable<PropertySection> Build(InspectionContext context)
    {
        var el = context.Element;
        var all = New("All properties", group: "allprops");

        foreach (var name in ReflectionLookup.ReadablePropertyNames(el.GetType()))
        {
            if (Skip.Contains(name))
                continue;
            if (ReflectionLookup.FindInstanceProperty(el.GetType(), name) is not { } pi)
                continue;

            var type = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;
            if (IsSupported(type))
                AddEditable(all, el, name);
        }

        if (all.Rows.Count == 0)
            yield break;

        var more = New("More");
        more.Rows.Add(new PropertyRow("All properties",
            $"{all.Rows.Count} propert{(all.Rows.Count == 1 ? "y" : "ies")}", TogglesGroup: "allprops"));
        yield return more;
        yield return all;
    }

    static bool IsSupported(Type type) =>
        type.IsPrimitive || type.IsEnum
        || type == typeof(string) || type == typeof(decimal)
        || type == typeof(Color) || type == typeof(Thickness) || type == typeof(CornerRadius)
        || type == typeof(Point) || type == typeof(Size) || type == typeof(Rect)
        || type == typeof(LayoutOptions) || type == typeof(GridLength)
        || type == typeof(TimeSpan) || type == typeof(DateTime)
        || typeof(Brush).IsAssignableFrom(type) || typeof(IShape).IsAssignableFrom(type);
}
