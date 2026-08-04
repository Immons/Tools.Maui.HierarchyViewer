namespace Immons.Tools.Maui.Inspector.Features.Properties;

/// <summary>
/// Builds zero or more property sections for the inspected element. Implementations are
/// stateless, cover exactly one concern, and are composed by <see cref="IPropertyCollector"/>
/// in display order — adding a section means adding an implementation, not editing existing code.
/// </summary>
internal interface IPropertySectionBuilder
{
    IEnumerable<PropertySection> Build(InspectionContext context);
}
