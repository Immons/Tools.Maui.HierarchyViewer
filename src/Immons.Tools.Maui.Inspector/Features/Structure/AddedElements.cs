using System.Runtime.CompilerServices;

namespace Immons.Tools.Maui.Inspector.Features.Structure;

/// <summary>Weak element → op map, so dropped subtrees never keep pages alive.</summary>
internal sealed class AddedElements : IAddedElements
{
    readonly ConditionalWeakTable<object, StructureOp> _ops = [];

    public void Register(VisualElement element, StructureOp op)
    {
        _ops.Remove(element);
        _ops.Add(element, op);
    }

    public void Unregister(VisualElement element) => _ops.Remove(element);

    public StructureOp? Find(object element) => _ops.TryGetValue(element, out var op) ? op : null;
}
