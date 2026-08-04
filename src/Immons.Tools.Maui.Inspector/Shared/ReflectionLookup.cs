using System.Reflection;

namespace Immons.Tools.Maui.Inspector.Shared;

internal static class ReflectionLookup
{
    /// <summary>
    /// Safe replacement for <c>Type.GetProperty(name)</c>: when a property is re-declared with
    /// <c>new</c> somewhere in the hierarchy, GetProperty throws AmbiguousMatchException — this
    /// walks from the runtime type up and returns the most-derived declaration instead.
    /// </summary>
    public static PropertyInfo? FindInstanceProperty(Type type, string name)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            var property = current.GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (property != null)
                return property;
        }
        return null;
    }

    /// <summary>The static "{Name}Property" BindableProperty of the type, if declared.</summary>
    public static BindableProperty? FindBindableProperty(Type type, string propertyName)
    {
        var field = type.GetField($"{propertyName}Property",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        return field?.GetValue(null) as BindableProperty;
    }

    /// <summary>Distinct readable instance-property names, ordered; hidden re-declarations collapse to one.</summary>
    public static IEnumerable<string> ReadablePropertyNames(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0 && p.CanRead)
            .Select(p => p.Name)
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal);
}
