using System.Globalization;
using static Immons.Tools.Maui.Inspector.Shared.ValueFormatter;

namespace Immons.Tools.Maui.Inspector.Features.Properties;

/// <summary>XAML-shorthand text form of GridLength ("Auto", "*", "2*", "48").</summary>
internal static class GridLengthText
{
    public static string Format(GridLength g) =>
        g.IsAuto ? "Auto"
        : g.IsStar ? (g.Value == 1 ? "*" : $"{F(g.Value)}*")
        : F(g.Value);

    public static bool TryParse(string text, out GridLength length)
    {
        length = GridLength.Auto;
        text = text.Trim();

        if (text.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return true;

        if (text.EndsWith('*'))
        {
            var factor = text[..^1].Trim();
            if (factor.Length == 0)
            {
                length = GridLength.Star;
                return true;
            }
            if (double.TryParse(factor, NumberStyles.Float, CultureInfo.InvariantCulture, out var star) && star > 0)
            {
                length = new GridLength(star, GridUnitType.Star);
                return true;
            }
            return false;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var abs) && abs >= 0)
        {
            length = new GridLength(abs);
            return true;
        }

        return false;
    }
}
