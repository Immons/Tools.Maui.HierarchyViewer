using System.Text.RegularExpressions;

namespace Immons.Tools.Maui.Inspector.Features.Editing;

/// <summary>
/// Parses "{OnPlatform iOS=20, Android=14, Default=16}" / "{OnIdiom Phone=…, Default=…}"
/// editor input and resolves the raw value for the current device. The expression itself
/// goes into XAML verbatim; the resolved value feeds the runtime editor pipeline.
/// </summary>
internal static class DeviceValueExpressionParser
{
    static readonly Regex Expression = new(@"^\{\s*(OnPlatform|OnIdiom)\s+(.+)\}\s*$",
        RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// False when the text is not an OnPlatform/OnIdiom expression. Otherwise true, with
    /// <paramref name="currentValue"/> set to the entry matching this device (bare or
    /// Default= entry as fallback) — null when the expression has no value for this device.
    /// </summary>
    public static bool TryResolve(string text, out string? currentValue)
    {
        currentValue = null;
        var match = Expression.Match(text.Trim());
        if (!match.Success)
            return false;

        var currentKey = match.Groups[1].Value == "OnPlatform" ? CurrentPlatformKey() : CurrentIdiomKey();
        string? defaultValue = null;

        foreach (var pair in SplitPairs(match.Groups[2].Value))
        {
            var eq = IndexOfTopLevel(pair, '=');
            if (eq < 0)
            {
                defaultValue = Unquote(pair); // bare leading value = Default
                continue;
            }

            var key = pair[..eq].Trim();
            var value = Unquote(pair[(eq + 1)..]);
            if (key.Equals("Default", StringComparison.OrdinalIgnoreCase))
                defaultValue = value;
            else if (key.Equals(currentKey, StringComparison.OrdinalIgnoreCase))
                currentValue = value;
        }

        currentValue ??= defaultValue;
        return true;
    }

    static string CurrentPlatformKey()
    {
        var platform = DeviceInfo.Current.Platform;
        if (platform == DevicePlatform.iOS) return "iOS";
        if (platform == DevicePlatform.Android) return "Android";
        if (platform == DevicePlatform.WinUI) return "WinUI";
        if (platform == DevicePlatform.MacCatalyst) return "MacCatalyst";
        return platform.ToString();
    }

    static string CurrentIdiomKey()
    {
        var idiom = DeviceInfo.Current.Idiom;
        if (idiom == DeviceIdiom.Phone) return "Phone";
        if (idiom == DeviceIdiom.Tablet) return "Tablet";
        if (idiom == DeviceIdiom.Desktop) return "Desktop";
        if (idiom == DeviceIdiom.TV) return "TV";
        if (idiom == DeviceIdiom.Watch) return "Watch";
        return idiom.ToString();
    }

    /// <summary>Splits on commas that sit outside single quotes and nested braces.</summary>
    static IEnumerable<string> SplitPairs(string text)
    {
        var depth = 0;
        var inQuote = false;
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '\'')
                inQuote = !inQuote;
            else if (!inQuote && ch == '{')
                depth++;
            else if (!inQuote && ch == '}')
                depth--;
            else if (!inQuote && depth == 0 && ch == ',')
            {
                yield return text[start..i].Trim();
                start = i + 1;
            }
        }
        if (start < text.Length)
            yield return text[start..].Trim();
    }

    static int IndexOfTopLevel(string text, char needle)
    {
        var inQuote = false;
        var depth = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '\'')
                inQuote = !inQuote;
            else if (!inQuote && ch == '{')
                depth++;
            else if (!inQuote && ch == '}')
                depth--;
            else if (!inQuote && depth == 0 && ch == needle)
                return i;
        }
        return -1;
    }

    static string Unquote(string value)
    {
        value = value.Trim();
        return value.Length >= 2 && value[0] == '\'' && value[^1] == '\''
            ? value[1..^1]
            : value;
    }
}
