namespace Immons.Tools.Maui.Inspector.Features.Properties;

internal sealed record PropertyRow(
    string Name,
    string Value,
    Color? Swatch = null,
    PropertyEditor? Editor = null,
    string? TogglesGroup = null,
    Action? Action = null,
    string? Binding = null,
    string? DeviceExpression = null,
    IReadOnlyList<string>? Resources = null);
