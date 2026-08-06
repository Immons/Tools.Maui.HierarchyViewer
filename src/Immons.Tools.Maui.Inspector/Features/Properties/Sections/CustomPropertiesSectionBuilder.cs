using System.Reflection;
using static Immons.Tools.Maui.Inspector.Features.Properties.SectionBuilder;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Sections;

/// <summary>
/// The element's OWN bindable properties — everything declared on types outside the framework
/// (custom controls), editable through the regular editors. Walks the hierarchy from the most
/// derived type down to the first Microsoft.Maui type, one section per declaring type when the
/// chain is deeper than one.
/// </summary>
internal sealed class CustomPropertiesSectionBuilder : IPropertySectionBuilder
{
    public IEnumerable<PropertySection> Build(InspectionContext context)
    {
        var element = context.Element;

        for (var type = element.GetType(); type != null && !IsFrameworkType(type); type = type.BaseType)
        {
            var names = DeclaredBindablePropertyNames(type);
            if (names.Count == 0)
                continue;

            var section = New($"{type.Name} properties");
            foreach (var name in names)
                AddEditable(section, element, name);

            if (section.Rows.Count > 0)
                yield return section;
        }
    }

    static bool IsFrameworkType(Type type) =>
        type.Namespace?.StartsWith("Microsoft.Maui", StringComparison.Ordinal) == true;

    /// <summary>Names of bindable properties declared directly on the type, in declaration order.</summary>
    static List<string> DeclaredBindablePropertyNames(Type type)
    {
        var names = new List<string>();
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (field.FieldType != typeof(BindableProperty))
                continue;

            BindableProperty? property;
            try
            {
                property = field.GetValue(null) as BindableProperty;
            }
            catch
            {
                continue;
            }

            // Attached properties have no matching CLR property on the element — skip them.
            if (property != null
                && ReflectionLookup.FindInstanceProperty(type, property.PropertyName) != null
                && !names.Contains(property.PropertyName))
                names.Add(property.PropertyName);
        }
        return names;
    }
}
