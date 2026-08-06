namespace Immons.Tools.Maui.Inspector.Features.Structure;

/// <summary>
/// Live elements created by the inspector, mapped to their persisted add-operation. The XAML
/// change log consults this before its usual SourceInfo path: an inspector-created element has
/// no source location, so its attribute edits update the pending insert snippet instead.
/// </summary>
internal interface IAddedElements
{
    void Register(VisualElement element, StructureOp op);

    void Unregister(VisualElement element);

    StructureOp? Find(object element);
}
