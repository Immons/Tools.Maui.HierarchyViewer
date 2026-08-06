using System.Reflection;
using Immons.Tools.Maui.Inspector.Features.Editing;

namespace Immons.Tools.Maui.Inspector.Features.Structure;

/// <summary>
/// Best-effort re-application of stored attribute strings when a persisted add is replayed.
/// Covers the value kinds the editors emit; anything unparsable is skipped, never thrown.
/// </summary>
internal static class AttributeApplier
{
    public static void Apply(VisualElement element, IReadOnlyDictionary<string, string> attributes)
    {
        foreach (var (name, value) in attributes)
            TrySet(element, name, value);
    }

    static void TrySet(VisualElement element, string name, string value)
    {
        try
        {
            var property = element.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property?.SetMethod == null)
                return;

            if (ValueParser.TryParse(property.PropertyType, KindOf(property.PropertyType), value, out var parsed))
                property.SetValue(element, parsed);
        }
        catch
        {
            // A stale attribute must never break page construction.
        }
    }

    static EditorKind KindOf(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(bool))
            return EditorKind.Bool;
        if (type == typeof(double) || type == typeof(int) || type == typeof(float))
            return EditorKind.Double;
        if (type == typeof(Color))
            return EditorKind.Color;
        if (type == typeof(Thickness))
            return EditorKind.Thickness;
        if (type == typeof(CornerRadius))
            return EditorKind.CornerRadius;
        if (type == typeof(Point))
            return EditorKind.Point;
        if (type == typeof(LayoutOptions))
            return EditorKind.LayoutOptions;
        if (type.IsEnum)
            return EditorKind.Enum;
        return EditorKind.Text;
    }
}
