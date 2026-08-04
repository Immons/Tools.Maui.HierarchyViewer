using System.Runtime.CompilerServices;

namespace Immons.Tools.Maui.Inspector.Features.VisualTree;

/// <summary>Weakly-held element ↔ id map; ids stay stable for the element's lifetime.</summary>
internal sealed class ElementRegistry : IElementRegistry
{
    const int CleanupThreshold = 4096;

    readonly ConditionalWeakTable<VisualElement, StrongBox<int>> _ids = new();
    readonly Dictionary<int, WeakReference<VisualElement>> _byId = [];
    int _next;

    public int GetId(VisualElement element)
    {
        lock (_byId)
        {
            if (_ids.TryGetValue(element, out var box))
                return box.Value;

            var id = ++_next;
            _ids.Add(element, new StrongBox<int>(id));
            _byId[id] = new WeakReference<VisualElement>(element);

            if (_byId.Count > CleanupThreshold)
                RemoveDeadEntries();

            return id;
        }
    }

    public VisualElement? Find(int id)
    {
        lock (_byId)
        {
            return _byId.TryGetValue(id, out var weak) && weak.TryGetTarget(out var element) ? element : null;
        }
    }

    void RemoveDeadEntries()
    {
        foreach (var dead in _byId.Where(kv => !kv.Value.TryGetTarget(out _)).Select(kv => kv.Key).ToList())
            _byId.Remove(dead);
    }
}
