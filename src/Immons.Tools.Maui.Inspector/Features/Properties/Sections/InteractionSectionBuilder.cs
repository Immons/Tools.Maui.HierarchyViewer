using static Immons.Tools.Maui.Inspector.Features.Properties.SectionBuilder;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Sections;

/// <summary>Enabled state, input transparency, gestures and focus.</summary>
internal sealed class InteractionSectionBuilder : IPropertySectionBuilder
{
    public IEnumerable<PropertySection> Build(InspectionContext context)
    {
        var el = context.Element;
        var s = New("Interaction");
        AddEditable(s, el, "IsEnabled");
        AddEditable(s, el, "InputTransparent");
        if (el is Layout)
            AddEditable(s, el, "CascadeInputTransparent");
        if (el is View { GestureRecognizers.Count: > 0 } view)
            Add(s, "Gestures", string.Join(", ", view.GestureRecognizers.Select(g => g.GetType().Name)));
        if (el.IsFocused)
            Add(s, "IsFocused", "True");
        yield return s;
    }
}
