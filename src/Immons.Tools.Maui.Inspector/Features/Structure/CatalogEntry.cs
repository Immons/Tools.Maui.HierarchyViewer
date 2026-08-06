namespace Immons.Tools.Maui.Inspector.Features.Structure;

/// <summary>One control offered by the "Add element" catalog.</summary>
internal sealed record CatalogEntry(
    string Name,
    string TypeName,
    string Description,
    bool IsContainer,
    bool IsCustom);
