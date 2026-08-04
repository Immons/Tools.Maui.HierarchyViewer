using static Immons.Tools.Maui.Inspector.Shared.ValueFormatter;
using static Immons.Tools.Maui.Inspector.Features.Properties.SectionBuilder;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Sections;

/// <summary>Measured geometry: size, position, window rect, desired size.</summary>
internal sealed class BoundsSectionBuilder : IPropertySectionBuilder
{
    public IEnumerable<PropertySection> Build(InspectionContext context)
    {
        var el = context.Element;
        var s = New("Bounds");
        Add(s, "Size", $"{F(el.Frame.Width)} × {F(el.Frame.Height)}");
        Add(s, "Position", $"{F(el.Frame.X)}, {F(el.Frame.Y)} (in parent)");
        if (context.WindowBounds is { } wb)
            Add(s, "Window rect", Format(wb));
        Add(s, "DesiredSize", Format(el.DesiredSize));
        yield return s;
    }
}
