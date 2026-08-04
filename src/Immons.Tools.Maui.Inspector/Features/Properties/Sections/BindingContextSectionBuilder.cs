using static Immons.Tools.Maui.Inspector.Features.Properties.SectionBuilder;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Sections;

/// <summary>Simple-typed properties of the element's BindingContext (view model),
/// editable where a public setter exists. Purely in-memory — never written to XAML.</summary>
internal sealed class BindingContextSectionBuilder : IPropertySectionBuilder
{
    public IEnumerable<PropertySection> Build(InspectionContext context)
    {
        if (context.Element.BindingContext is not { } bindingContext)
            yield break;

        var vm = BuildViewModelSection(bindingContext);
        if (vm.Rows.Count == 0)
            yield break;

        var toggle = New("ViewModel");
        toggle.Rows.Add(new PropertyRow("BindingContext",
            $"{bindingContext.GetType().Name} ({vm.Rows.Count})", TogglesGroup: "viewmodel"));
        yield return toggle;
        yield return vm;
    }

    static PropertySection BuildViewModelSection(object bindingContext)
    {
        var s = New($"ViewModel: {bindingContext.GetType().Name}", group: "viewmodel");

        foreach (var name in ReflectionLookup.ReadablePropertyNames(bindingContext.GetType()))
        {
            if (ReflectionLookup.FindInstanceProperty(bindingContext.GetType(), name) is not { } pi)
                continue;

            var type = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;

            if (typeof(System.Windows.Input.ICommand).IsAssignableFrom(type))
            {
                Add(s, name, "(command)");
                continue;
            }

            if (IsSimple(type))
                AddEditable(s, bindingContext, name);
        }

        return s;
    }

    static bool IsSimple(Type type) =>
        type.IsPrimitive || type.IsEnum
        || type == typeof(string) || type == typeof(decimal)
        || type == typeof(DateTime) || type == typeof(TimeSpan)
        || type == typeof(Color) || type == typeof(Thickness);
}
