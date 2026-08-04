namespace Immons.Tools.Maui.Inspector.Features.Properties;

/// <summary>Shared helpers for the section builders.</summary>
internal static class SectionBuilder
{
    public static PropertySection New(string title, string? group = null) => new(title, [], group);

    public static void AddIfAny(List<PropertySection> sections, PropertySection section)
    {
        if (section.Rows.Count > 0)
            sections.Add(section);
    }

    public static void Add(PropertySection s, string name, string value, Color? swatch = null) =>
        s.Rows.Add(new PropertyRow(name, value, swatch));

    /// <summary>
    /// Adds a row backed by a public CLR property of the element; the row becomes editable
    /// when the property has a public setter of a supported type.
    /// </summary>
    public static void AddEditable(PropertySection s, object el, string property, string? label = null)
    {
        var pi = ReflectionLookup.FindInstanceProperty(el.GetType(), property);
        if (pi == null)
            return;

        object? raw = null;
        try { raw = pi.GetValue(el); }
        catch { /* getter threw — show the row as empty */ }

        s.Rows.Add(new PropertyRow(
            label ?? property,
            ValueFormatter.FormatValue(raw),
            raw as Color,
            EditorFactory.Clr(el, property),
            Binding: BindingDescriptor.Describe(el, property),
            DeviceExpression: InspectorServices.Expressions.Find(el, property),
            Resources: SuggestionsFor(el, property, Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType)));
    }

    /// <summary>
    /// Ready-to-use editor suggestions for a property: registered font aliases for FontFamily,
    /// otherwise "{StaticResource Key}" for resources whose value fits the property type.
    /// </summary>
    static IReadOnlyList<string> SuggestionsFor(object target, string property, Type propertyType)
    {
        if (property.Contains("FontFamily", StringComparison.Ordinal))
        {
            var fonts = FontCatalog.RegisteredAliases();
            return fonts.Count > 0 ? fonts : [];
        }

        return ResourceLookup.CompatibleKeys(target, propertyType)
            .Select(key => $"{{StaticResource {key}}}")
            .ToList();
    }

    /// <summary>Editable integer row backed by get/set delegates (attached properties like Grid.Row).</summary>
    public static void AddAttachedInt(PropertySection s, string name, Func<int> get, Action<int> set, int min, object target)
    {
        var editor = new PropertyEditor(EditorKind.Double, null, text =>
        {
            if (!int.TryParse(text.Trim(), out var value) || value < min)
                return false;
            try
            {
                set(value);
                return true;
            }
            catch
            {
                return false;
            }
        })
        {
            XamlTarget = target,
            XamlAttribute = name,
        };
        s.Rows.Add(new PropertyRow(name, get().ToString(), null, editor));
    }
}
