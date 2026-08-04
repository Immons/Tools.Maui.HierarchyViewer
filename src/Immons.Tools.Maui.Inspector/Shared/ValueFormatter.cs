using System.Globalization;
using Microsoft.Maui.Controls.Shapes;

namespace Immons.Tools.Maui.Inspector.Shared;

internal static class ValueFormatter
{
    /// <summary>Formats a double compactly: integers without decimals, otherwise up to two.</summary>
    public static string F(double value)
    {
        if (double.IsPositiveInfinity(value)) return "∞";
        if (double.IsNegativeInfinity(value)) return "-∞";
        if (double.IsNaN(value)) return "NaN";
        return value == Math.Truncate(value)
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public static string Format(Thickness t)
    {
        if (t.Left == t.Right && t.Top == t.Bottom && t.Left == t.Top)
            return F(t.Left);
        if (t.Left == t.Right && t.Top == t.Bottom)
            return $"{F(t.Left)}, {F(t.Top)}";
        return $"{F(t.Left)}, {F(t.Top)}, {F(t.Right)}, {F(t.Bottom)}";
    }

    public static string Format(CornerRadius r) =>
        r.TopLeft == r.TopRight && r.TopLeft == r.BottomLeft && r.TopLeft == r.BottomRight
            ? F(r.TopLeft)
            : $"{F(r.TopLeft)}, {F(r.TopRight)}, {F(r.BottomLeft)}, {F(r.BottomRight)}";

    public static string Format(Color? c)
    {
        if (c == null)
            return "–";
        var (r, g, b, a) = ((byte)(c.Red * 255), (byte)(c.Green * 255), (byte)(c.Blue * 255), (byte)(c.Alpha * 255));
        return a == 255 ? $"#{r:X2}{g:X2}{b:X2}" : $"#{a:X2}{r:X2}{g:X2}{b:X2}";
    }

    public static string Format(LayoutOptions o) => o.Alignment.ToString();

    public static string Format(Rect r) => $"{F(r.X)}, {F(r.Y)}  {F(r.Width)} × {F(r.Height)}";

    public static string Format(Size s) => $"{F(s.Width)} × {F(s.Height)}";

    public static string Format(Brush? brush) => brush switch
    {
        null => "–",
        SolidColorBrush s => Format(s.Color),
        GradientBrush g => $"{g.GetType().Name} ({g.GradientStops.Count} stops)",
        _ => brush.GetType().Name,
    };

    // Kept parseable by StrokeShapeTypeConverter ("RoundRectangle 12", "Ellipse", …).
    public static string Format(IShape? shape) => shape switch
    {
        null => "–",
        RoundRectangle r => $"RoundRectangle {Format(r.CornerRadius)}",
        Ellipse => "Ellipse",
        Rectangle => "Rectangle",
        _ => shape.GetType().Name,
    };

    public static string Format(Microsoft.Maui.Font f)
    {
        var parts = new List<string> { f.Family ?? "(default)" };
        parts.Add(f.Size > 0 ? F(f.Size) : "(default size)");
        if (f.Weight != FontWeight.Regular)
            parts.Add(f.Weight.ToString());
        if (f.Slant != FontSlant.Default)
            parts.Add(f.Slant.ToString());
        return string.Join(", ", parts);
    }

    /// <summary>Formats any property value for display (colors, thicknesses, brushes, shapes…).</summary>
    public static string FormatValue(object? value) => value switch
    {
        null => "",
        string s => s,
        double d when double.IsNaN(d) => "",
        double d => F(d),
        float f => F(f),
        int i => i.ToString(),
        bool b => b.ToString(),
        Thickness t => Format(t),
        CornerRadius c => Format(c),
        Point p => $"{F(p.X)}, {F(p.Y)}",
        Color c => Format(c),
        LayoutOptions o => Format(o),
        SolidColorBrush b => Format(b.Color),
        Brush b => Format(b),
        IShape shape => Format(shape),
        _ when value.GetType().Name == "AppThemeBinding" => FormatAppThemeBinding(value),
        _ => value.ToString() ?? "",
    };

    // AppThemeBinding is internal — read Light/Dark by reflection.
    static string FormatAppThemeBinding(object binding)
    {
        var type = binding.GetType();
        object? Get(string name) => type.GetProperty(name)?.GetValue(binding);
        return $"Light={FormatValue(Get("Light"))}, Dark={FormatValue(Get("Dark"))}";
    }

    /// <summary>
    /// Replaces Private Use Area characters (icon-font glyphs — FontAwesome etc.) with their
    /// "\uXXXX" escape, so dumps and tree labels show a searchable codepoint instead of tofu.
    /// </summary>
    public static string EscapeIconGlyphs(string s)
    {
        if (!s.Any(IsPrivateUse))
            return s;

        var sb = new System.Text.StringBuilder(s.Length + 8);
        foreach (var ch in s)
        {
            if (IsPrivateUse(ch))
                sb.Append($"\\u{(int)ch:X4}");
            else
                sb.Append(ch);
        }
        return sb.ToString();

        static bool IsPrivateUse(char ch) => ch is >= '\uE000' and <= '\uF8FF';
    }

    public static string Truncate(string? s, int max = 100)
    {
        if (string.IsNullOrEmpty(s))
            return "–";
        s = s.Replace('\n', '⏎');
        return s.Length > max ? s[..max] + "…" : s;
    }
}
