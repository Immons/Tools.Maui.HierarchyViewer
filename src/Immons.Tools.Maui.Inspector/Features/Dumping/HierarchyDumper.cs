using System.Text;
using Microsoft.Maui.Controls.Shapes;
using static Immons.Tools.Maui.Inspector.Shared.ValueFormatter;

namespace Immons.Tools.Maui.Inspector.Features.Dumping;

/// <summary>
/// Produces an indented, designer-oriented text dump of the visual tree:
/// window-space positions and sizes, margins, paddings, spacings, measured gaps
/// to the previous sibling, fonts and colors — for comparing an app against its design.
/// </summary>
internal static class HierarchyDumper
{
    public static string Dump(IEnumerable<VisualElement> roots, Func<VisualElement, Rect?> bounds, Size windowSize)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"══════ MauiInspector dump ══════ window {F(windowSize.Width)}×{F(windowSize.Height)} dp, positions are x,y w×h in window coordinates");

        foreach (var root in roots)
            DumpNode(sb, root, bounds, 0, null);

        sb.AppendLine("══════ end of dump ══════");
        return sb.ToString();
    }

    static void DumpNode(StringBuilder sb, VisualElement el, Func<VisualElement, Rect?> bounds, int depth, Rect? prevSiblingRect)
    {
        sb.Append(' ', depth * 2);
        sb.Append(Describe(el));

        var rect = bounds(el);
        if (rect is { } r)
            sb.Append($"  {F(r.X)},{F(r.Y)} {F(r.Width)}×{F(r.Height)}");
        else
            sb.Append("  (not attached)");

        AppendDetails(sb, el, rect, prevSiblingRect);
        sb.AppendLine();

        Rect? prev = null;
        foreach (var child in VisualTreeWalker.GetVisualChildren(el))
        {
            DumpNode(sb, child, bounds, depth + 1, prev);
            prev = bounds(child) ?? prev;
        }
    }

    static string Describe(VisualElement el)
    {
        var name = ElementInfo.ShortLabel(el);

        var text = el switch
        {
            Label { FormattedText: { } ft } => string.Concat(ft.Spans.Select(s => s.Text)),
            IText t => t.Text,
            _ => null,
        };
        if (!string.IsNullOrEmpty(text))
        {
            // Full text on purpose — the dump is meant for design comparison.
            text = ValueFormatter.EscapeIconGlyphs(text.Replace('\n', ' '));
            name += $" \"{text}\"";
        }

        return name;
    }

    static void AppendDetails(StringBuilder sb, VisualElement el, Rect? rect, Rect? prevSiblingRect)
    {
        if (!el.IsVisible)
            sb.Append("  [hidden]");

        if (rect is { } r && prevSiblingRect is { } prev)
            AppendGap(sb, r, prev);

        if (el is View view)
        {
            if (view.Margin != new Thickness(0))
                sb.Append($"  margin={Format(view.Margin)}");

            var h = view.HorizontalOptions.Alignment;
            var v = view.VerticalOptions.Alignment;
            if (h != LayoutAlignment.Fill || v != LayoutAlignment.Fill)
                sb.Append($"  align={h},{v}");
        }

        if (ElementInfo.GetPadding(el) is { } padding && padding != new Thickness(0))
            sb.Append($"  padding={Format(padding)}");

        switch (el)
        {
            case StackBase stack:
                sb.Append($"  spacing={F(stack.Spacing)}");
                break;
            case Grid grid:
                sb.Append($"  spacing={F(grid.RowSpacing)}/{F(grid.ColumnSpacing)}");
                sb.Append($"  rows/cols={grid.RowDefinitions.Count}/{grid.ColumnDefinitions.Count}");
                break;
        }

        if (el is ITextStyle ts)
        {
            var f = ts.Font;
            var font = $"{f.Family ?? "default"} {F(f.Size)}";
            if (f.Weight != FontWeight.Regular)
                font += $" {f.Weight}";
            if (f.Slant != FontSlant.Default)
                font += " Italic";
            sb.Append($"  font={font}");
            if (ts.TextColor is { } tc)
                sb.Append($"  color={Format(tc)}");
        }

        var bg = el.BackgroundColor ?? (el.Background as SolidColorBrush)?.Color;
        if (bg != null)
            sb.Append($"  bg={Format(bg)}");

        switch (el)
        {
            case Border border:
                if (border.StrokeShape is RoundRectangle rr)
                    sb.Append($"  corner={Format(rr.CornerRadius)}");
                if (border.StrokeThickness > 0 && (border.Stroke as SolidColorBrush)?.Color is { } sc)
                    sb.Append($"  stroke={Format(sc)}/{F(border.StrokeThickness)}");
                break;
            case Button button:
                if (button.CornerRadius > 0)
                    sb.Append($"  corner={button.CornerRadius}");
                break;
            case BoxView box:
                if (box.CornerRadius != new CornerRadius(0))
                    sb.Append($"  corner={Format(box.CornerRadius)}");
                if (box.Color is { } bc)
                    sb.Append($"  color={Format(bc)}");
                break;
            case Image image:
                sb.Append($"  aspect={image.Aspect}");
                break;
        }

        if (el.Opacity < 1)
            sb.Append($"  opacity={F(el.Opacity)}");
    }

    /// <summary>Measured distance to the previous sibling: vertical when they overlap
    /// horizontally, horizontal when they overlap vertically.</summary>
    static void AppendGap(StringBuilder sb, Rect r, Rect prev)
    {
        var overlapX = Math.Min(r.Right, prev.Right) - Math.Max(r.Left, prev.Left);
        var overlapY = Math.Min(r.Bottom, prev.Bottom) - Math.Max(r.Top, prev.Top);

        if (overlapX > 0 && overlapX >= overlapY)
        {
            var gap = r.Top - prev.Bottom;
            if (gap >= 0)
                sb.Append($"  gapAbove={F(gap)}");
        }
        else if (overlapY > 0)
        {
            var gap = r.Left - prev.Right;
            if (gap >= 0)
                sb.Append($"  gapLeft={F(gap)}");
        }
    }
}
