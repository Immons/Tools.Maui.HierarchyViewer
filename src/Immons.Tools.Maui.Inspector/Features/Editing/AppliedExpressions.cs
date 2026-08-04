using System.Runtime.CompilerServices;

namespace Immons.Tools.Maui.Inspector.Features.Editing;

/// <summary>Weakly keyed by the edited object — entries die with the element.</summary>
internal sealed class AppliedExpressions : IAppliedExpressions
{
    readonly ConditionalWeakTable<object, Dictionary<string, string>> _byTarget = new();

    public void Record(object target, string property, string? expression)
    {
        var map = _byTarget.GetOrCreateValue(target);
        lock (map)
        {
            if (expression == null)
                map.Remove(property);
            else
                map[property] = expression;
        }
    }

    public string? Find(object target, string property)
    {
        if (!_byTarget.TryGetValue(target, out var map))
            return null;
        lock (map)
        {
            return map.GetValueOrDefault(property);
        }
    }
}
