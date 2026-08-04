namespace Immons.Tools.Maui.Inspector.Features.Properties;

/// <summary>Builds the property sections shown for a selected element.</summary>
internal interface IPropertyCollector
{
    List<PropertySection> Collect(VisualElement element, Rect? windowBounds);
}
