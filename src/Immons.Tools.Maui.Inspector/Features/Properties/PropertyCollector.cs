namespace Immons.Tools.Maui.Inspector.Features.Properties;

/// <summary>Composes the section builders in display order.</summary>
internal sealed class PropertyCollector(IReadOnlyList<IPropertySectionBuilder> builders) : IPropertyCollector
{
    public PropertyCollector(IXamlChangeLog xamlChanges)
        : this(
        [
            new ElementSectionBuilder(),
            new StyleSectionBuilder(),
            new BoundsSectionBuilder(),
            new LayoutSectionBuilder(),
            new GridDefinitionsSectionBuilder(xamlChanges),
            new TextSectionBuilder(),
            new SpanSectionBuilder(),
            new AppearanceSectionBuilder(),
            new TransformSectionBuilder(),
            new InteractionSectionBuilder(),
            new AccessibilitySectionBuilder(),
            new ControlSectionBuilder(),
            new CustomPropertiesSectionBuilder(),
            new BindingContextSectionBuilder(),
            new AllPropertiesSectionBuilder(),
        ])
    {
    }

    public List<PropertySection> Collect(VisualElement element, Rect? windowBounds)
    {
        var context = new InspectionContext(element, windowBounds);
        var sections = new List<PropertySection>();
        foreach (var builder in builders)
        {
            try
            {
                sections.AddRange(builder.Build(context).Where(s => s.Rows.Count > 0));
            }
            catch
            {
                // One broken section (custom controls, exotic reflection) must not
                // take down the whole property panel.
            }
        }
        return sections;
    }
}
