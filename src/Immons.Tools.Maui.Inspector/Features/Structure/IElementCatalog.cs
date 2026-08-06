namespace Immons.Tools.Maui.Inspector.Features.Structure;

/// <summary>Controls that can be added to the tree: curated MAUI built-ins plus the app's own views.</summary>
internal interface IElementCatalog
{
    IReadOnlyList<CatalogEntry> All();

    Type? Resolve(string typeName);
}
