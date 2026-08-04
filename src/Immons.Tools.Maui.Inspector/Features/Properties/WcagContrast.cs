namespace Immons.Tools.Maui.Inspector.Features.Properties;

/// <summary>WCAG 2.x contrast-ratio math.</summary>
internal static class WcagContrast
{
    public static double Ratio(Color a, Color b)
    {
        var l1 = RelativeLuminance(a);
        var l2 = RelativeLuminance(b);
        var (bright, dark) = l1 >= l2 ? (l1, l2) : (l2, l1);
        return (bright + 0.05) / (dark + 0.05);
    }

    public static string Rating(double ratio) =>
        ratio >= 7 ? "AAA" : ratio >= 4.5 ? "AA" : ratio >= 3 ? "AA large-text only" : "FAIL";

    /// <summary>Nearest ancestor solid background; white when nothing declares one.</summary>
    public static Color FindEffectiveBackground(VisualElement el, out bool assumed)
    {
        for (Element? current = el; current != null; current = current.Parent)
        {
            if (current is VisualElement ve)
            {
                var color = ve.BackgroundColor ?? (ve.Background as SolidColorBrush)?.Color;
                if (color is { Alpha: > 0.1f })
                {
                    assumed = false;
                    return color;
                }
            }
        }
        assumed = true;
        return Colors.White;
    }

    static double RelativeLuminance(Color c)
    {
        static double Linear(double channel) =>
            channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        return 0.2126 * Linear(c.Red) + 0.7152 * Linear(c.Green) + 0.0722 * Linear(c.Blue);
    }
}
