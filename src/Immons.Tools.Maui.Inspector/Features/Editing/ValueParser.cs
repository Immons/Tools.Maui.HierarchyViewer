using System.Globalization;
using System.Reflection;

namespace Immons.Tools.Maui.Inspector.Features.Editing;

/// <summary>Parses editor text input into property values (culture-invariant).</summary>
internal static class ValueParser
{
    public static bool TryParse(Type type, EditorKind kind, string text, out object? value)
    {
        value = null;
        text = text.Trim();

        switch (kind)
        {
            case EditorKind.Bool:
                if (bool.TryParse(text, out var b))
                {
                    value = b;
                    return true;
                }
                return false;

            case EditorKind.Double:
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                {
                    value = Convert.ChangeType(d, type, CultureInfo.InvariantCulture);
                    return true;
                }
                return false;

            case EditorKind.Text:
                value = text;
                return true;

            case EditorKind.Color:
                value = ParseColor(text);
                return value != null;

            case EditorKind.Thickness:
                if (ParseComponents(text) is { } t)
                {
                    value = t.Length switch
                    {
                        1 => new Thickness(t[0]),
                        2 => new Thickness(t[0], t[1]),
                        4 => new Thickness(t[0], t[1], t[2], t[3]),
                        _ => (object?)null,
                    };
                }
                return value != null;

            case EditorKind.CornerRadius:
                if (ParseComponents(text) is { } c)
                {
                    value = c.Length switch
                    {
                        1 => new CornerRadius(c[0]),
                        4 => new CornerRadius(c[0], c[1], c[2], c[3]),
                        _ => (object?)null,
                    };
                }
                return value != null;

            case EditorKind.Point:
                if (ParseComponents(text) is { } p)
                {
                    value = p.Length switch
                    {
                        1 => new Point(p[0], p[0]),
                        2 => new Point(p[0], p[1]),
                        _ => (object?)null,
                    };
                }
                return value != null;

            case EditorKind.LayoutOptions:
                value = text.ToLowerInvariant() switch
                {
                    "start" => LayoutOptions.Start,
                    "center" => LayoutOptions.Center,
                    "end" => LayoutOptions.End,
                    "fill" => LayoutOptions.Fill,
                    _ => (object?)null,
                };
                return value != null;

            case EditorKind.Enum:
                if (Enum.TryParse(type, text, ignoreCase: true, out var e))
                {
                    value = e;
                    return true;
                }
                return false;

            case EditorKind.Image:
                if (text.Length == 0)
                    return false;
                // Same convention as XAML's ImageSourceConverter: URI or bundled file name.
                value = Uri.TryCreate(text, UriKind.Absolute, out var uri) && !uri.IsFile
                    ? ImageSource.FromUri(uri)
                    : ImageSource.FromFile(text);
                return true;
        }

        return false;
    }

    /// <summary>Parses "#RRGGBB", "#AARRGGBB", a named color or "{StaticResource Key}"; null when invalid.</summary>
    public static Color? ParseColorValue(string text, object? context = null)
    {
        if (context != null && ResourceResolver.Resolve(context, text) is { } resource)
        {
            return resource switch
            {
                Color c => c,
                SolidColorBrush b => b.Color,
                _ => null,
            };
        }
        return ParseColor(text.Trim());
    }

    static Color? ParseColor(string text)
    {
        if (text.Length == 0)
            return null;

        if (text.StartsWith('#'))
        {
            try { return Color.FromArgb(text); }
            catch { return null; }
        }

        // Named colors: Colors.Red, Colors.CornflowerBlue, …
        var field = typeof(Colors).GetField(text,
            BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
        return field?.GetValue(null) as Color;
    }

    static double[]? ParseComponents(string text)
    {
        var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var result = new double[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out result[i]))
                return null;
        }
        return result.Length > 0 ? result : null;
    }
}
