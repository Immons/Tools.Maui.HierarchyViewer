using System.Globalization;
using System.Text.RegularExpressions;

namespace Immons.Tools.Maui.Inspector.Features.Editing;

/// <summary>Resolves "{StaticResource Key}" editor input against the app's resource dictionaries.</summary>
internal static class ResourceResolver
{
    static readonly Regex ResourceReference = new(
        @"^\{\s*(?:StaticResource|DynamicResource)\s+([\w.\-:]+)\s*\}$", RegexOptions.Compiled);

    /// <summary>
    /// Resolves "{StaticResource Key}" / "{DynamicResource Key}" input against the element's
    /// resource chain and the application resources; null when the text is not a resource
    /// reference or the key is unknown.
    /// </summary>
    public static object? Resolve(object? context, string text) =>
        IsResourceReference(text, out var key) && ResourceLookup.TryFind(context, key, out var value)
            ? value
            : null;

    /// <summary>True when the text is a "{StaticResource …}" / "{DynamicResource …}" expression.</summary>
    public static bool IsResourceReference(string text, out string key)
    {
        var match = ResourceReference.Match(text.Trim());
        key = match.Success ? match.Groups[1].Value : "";
        return match.Success;
    }

    /// <summary>Adapts a resolved resource to the property type (Color↔Brush, numeric conversions).</summary>
    public static object? Coerce(object resource, Type target)
    {
        if (target.IsInstanceOfType(resource))
            return resource;
        if (target == typeof(Color) && resource is SolidColorBrush brush)
            return brush.Color;
        if (typeof(Brush).IsAssignableFrom(target) && resource is Color color)
            return new SolidColorBrush(color);
        try
        {
            return Convert.ChangeType(resource, target, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}
