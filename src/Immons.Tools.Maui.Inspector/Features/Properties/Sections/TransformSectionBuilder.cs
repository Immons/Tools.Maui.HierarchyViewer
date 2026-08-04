using static Immons.Tools.Maui.Inspector.Features.Properties.SectionBuilder;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Sections;

/// <summary>Translation, rotation, scale and anchors (non-defaults only).</summary>
internal sealed class TransformSectionBuilder : IPropertySectionBuilder
{
    public IEnumerable<PropertySection> Build(InspectionContext context)
    {
        var el = context.Element;
        var s = New("Transform");
        AddEditable(s, el, "TranslationX");
        AddEditable(s, el, "TranslationY");
        AddEditable(s, el, "Rotation");
        AddEditable(s, el, "Scale");
        if (el.RotationX != 0)
            AddEditable(s, el, "RotationX");
        if (el.RotationY != 0)
            AddEditable(s, el, "RotationY");
        if (el.ScaleX != 1)
            AddEditable(s, el, "ScaleX");
        if (el.ScaleY != 1)
            AddEditable(s, el, "ScaleY");
        if (el.AnchorX != 0.5)
            AddEditable(s, el, "AnchorX");
        if (el.AnchorY != 0.5)
            AddEditable(s, el, "AnchorY");
        yield return s;
    }
}
