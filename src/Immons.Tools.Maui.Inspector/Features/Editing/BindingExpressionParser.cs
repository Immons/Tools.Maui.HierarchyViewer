using System.Text.RegularExpressions;

namespace Immons.Tools.Maui.Inspector.Features.Editing;

/// <summary>
/// Parses "{Binding Path[, Mode=…][, StringFormat='…']}" typed into an editor into a live
/// Binding. Converters and Sources cannot be instantiated from text — such input is rejected
/// so the field turns red instead of silently applying a broken binding.
/// </summary>
internal static class BindingExpressionParser
{
    static readonly Regex Expression = new(@"^\{\s*Binding\s+([^}]+)\}$", RegexOptions.Compiled);

    /// <summary>Any "{…}" markup — recorded verbatim into XAML instead of the runtime value.</summary>
    public static bool LooksLikeMarkup(string text) =>
        text.StartsWith('{') && text.EndsWith('}');

    public static BindingBase? TryParse(string text)
    {
        var match = Expression.Match(text.Trim());
        if (!match.Success)
            return null;

        string? path = null;
        var mode = BindingMode.Default;
        string? stringFormat = null;

        var parts = match.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var eq = part.IndexOf('=');
            if (eq < 0)
            {
                if (i != 0)
                    return null; // bare token allowed only as the leading path
                path = part;
                continue;
            }

            var key = part[..eq].Trim();
            var value = part[(eq + 1)..].Trim();
            switch (key)
            {
                case "Path":
                    path = value;
                    break;
                case "Mode" when Enum.TryParse<BindingMode>(value, true, out var parsedMode):
                    mode = parsedMode;
                    break;
                case "StringFormat":
                    stringFormat = value.Trim('\'', '"');
                    break;
                default:
                    return null; // Converter=/Source=… — not constructible from text
            }
        }

        return string.IsNullOrEmpty(path) ? null : new Binding(path, mode, stringFormat: stringFormat);
    }
}
