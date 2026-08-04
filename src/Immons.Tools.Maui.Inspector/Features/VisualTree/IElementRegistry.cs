namespace Immons.Tools.Maui.Inspector.Features.VisualTree;

/// <summary>Stable integer ids for visual elements, used by the web client.</summary>
internal interface IElementRegistry
{
    int GetId(VisualElement element);

    VisualElement? Find(int id);
}
