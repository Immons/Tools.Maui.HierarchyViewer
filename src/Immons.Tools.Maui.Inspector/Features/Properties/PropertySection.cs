namespace Immons.Tools.Maui.Inspector.Features.Properties;

internal sealed record PropertySection(string Title, List<PropertyRow> Rows, string? Group = null);
