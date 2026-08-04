namespace Immons.Tools.Maui.Inspector.Features.Properties;

/// <summary>Input for the section builders: the inspected element and its window-space bounds.</summary>
internal sealed record InspectionContext(VisualElement Element, Rect? WindowBounds);
