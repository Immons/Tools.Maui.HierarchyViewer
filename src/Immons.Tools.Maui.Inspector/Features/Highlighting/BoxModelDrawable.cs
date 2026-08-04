using Microsoft.Maui.Graphics;

namespace Immons.Tools.Maui.Inspector.Features.Highlighting;

/// <summary>Draws the margin/padding/content box model, optional compare outline, dashed guides, dimensions and distance badges.</summary>
internal sealed class BoxModelDrawable : IDrawable
{
    public BoxModel? Model { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Model is not { } m)
            return;

        if (m.Guides is { Count: > 0 } guides)
            DrawGuides(canvas, guides);

        if (m.BoundsRect.IsEmpty)
            return; // guides-only mode (nothing selected)

        var margin = ToRectF(m.MarginRect);
        var bounds = ToRectF(m.BoundsRect);
        var content = ToRectF(m.ContentRect);

        FillBetween(canvas, margin, bounds, Theme.MarginFill);
        FillBetween(canvas, bounds, content, Theme.PaddingFill);

        canvas.FillColor = Theme.ContentFill;
        canvas.FillRectangle(content);

        // Dashed guides extending the element edges across the whole screen.
        canvas.StrokeColor = Theme.Guide;
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = [4, 4];
        canvas.DrawLine(dirtyRect.Left, bounds.Top, dirtyRect.Right, bounds.Top);
        canvas.DrawLine(dirtyRect.Left, bounds.Bottom, dirtyRect.Right, bounds.Bottom);
        canvas.DrawLine(bounds.Left, dirtyRect.Top, bounds.Left, dirtyRect.Bottom);
        canvas.DrawLine(bounds.Right, dirtyRect.Top, bounds.Right, dirtyRect.Bottom);
        canvas.StrokeDashPattern = null;

        canvas.StrokeColor = Theme.Outline;
        canvas.StrokeSize = 1.5f;
        canvas.DrawRectangle(bounds);

        if (m.CompareBounds is { } compare)
        {
            var c = ToRectF(compare);
            canvas.FillColor = Theme.CompareFill;
            canvas.FillRectangle(c);
            canvas.StrokeColor = Theme.CompareOutline;
            canvas.StrokeSize = 1.5f;
            canvas.StrokeDashPattern = [6, 3];
            canvas.DrawRectangle(c);
            canvas.StrokeDashPattern = null;
        }

        DrawDimensionsBadge(canvas, dirtyRect, margin, m.Dimensions);

        if (m.Distances is { Count: > 0 } distances)
            DrawDistances(canvas, dirtyRect, distances);
    }

    // Flutter-style "debug paint": outline every element, color cycling by tree depth.
    static readonly Color[] GuideColors =
    [
        Color.FromArgb("#8055C9FF"),
        Color.FromArgb("#807FD48A"),
        Color.FromArgb("#80E8A33D"),
        Color.FromArgb("#80E08585"),
    ];

    static void DrawGuides(ICanvas canvas, IReadOnlyList<(Rect Rect, int Depth)> guides)
    {
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = null;
        foreach (var (rect, depth) in guides)
        {
            canvas.StrokeColor = GuideColors[depth % GuideColors.Length];
            canvas.DrawRectangle(ToRectF(rect));
        }
    }

    static void DrawDistances(ICanvas canvas, RectF dirtyRect, IReadOnlyList<DistanceSegment> distances)
    {
        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
        canvas.FontSize = 11;

        foreach (var seg in distances)
            DrawDistanceLine(canvas, seg);

        // Place each label at the midpoint of its line (H above, V to the side).
        // Light collision nudge only — keep badges glued to their segment.
        var badges = new List<(RectF Rect, string Text)>(distances.Count);
        foreach (var seg in distances)
        {
            var size = canvas.GetStringSize(seg.Label, Microsoft.Maui.Graphics.Font.Default, 11);
            var w = size.Width + 14;
            var h = size.Height + 6;
            badges.Add((DistanceBadgeLayout.Preferred(seg, dirtyRect, w, h), seg.Label));
        }

        DistanceBadgeLayout.NudgeOverlaps(badges, dirtyRect);

        foreach (var (rect, text) in badges)
            DrawBadgeRect(canvas, rect, text, Theme.DistanceLabelBg);
    }

    static void DrawDistanceLine(ICanvas canvas, DistanceSegment seg)
    {
        var x1 = (float)seg.From.X;
        var y1 = (float)seg.From.Y;
        var x2 = (float)seg.To.X;
        var y2 = (float)seg.To.Y;

        canvas.StrokeColor = Theme.DistanceLine;
        canvas.StrokeSize = 1.5f;
        canvas.StrokeDashPattern = null;
        canvas.DrawLine(x1, y1, x2, y2);

        const float cap = 4f;
        if (seg.Horizontal)
        {
            canvas.DrawLine(x1, y1 - cap, x1, y1 + cap);
            canvas.DrawLine(x2, y2 - cap, x2, y2 + cap);
        }
        else
        {
            canvas.DrawLine(x1 - cap, y1, x1 + cap, y1);
            canvas.DrawLine(x2 - cap, y2, x2 + cap, y2);
        }
    }

    static void DrawDimensionsBadge(ICanvas canvas, RectF dirtyRect, RectF anchor, string text)
    {
        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
        canvas.FontSize = 11;

        var size = canvas.GetStringSize(text, Microsoft.Maui.Graphics.Font.Default, 11);
        var w = size.Width + 14;
        var h = size.Height + 8;

        var x = anchor.Center.X - w / 2;
        var y = anchor.Top - h - 4;
        if (y < dirtyRect.Top + 4)
            y = anchor.Bottom + 4;
        if (y + h > dirtyRect.Bottom - 4)
            y = anchor.Top + 4;

        DrawBadgeRect(canvas, DistanceBadgeLayout.Clamp(x, y, w, h, dirtyRect), text, Theme.DimLabelBg);
    }

    static void DrawBadgeRect(ICanvas canvas, RectF badge, string text, Color bg)
    {
        canvas.FillColor = bg;
        canvas.FillRoundedRectangle(badge, 4);
        canvas.FontColor = Colors.White;
        canvas.DrawString(text, badge, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    static void FillBetween(ICanvas canvas, RectF outer, RectF inner, Color color)
    {
        if (outer == inner)
            return;

        var path = new PathF();
        path.AppendRectangle(outer);
        path.AppendRectangle(inner);
        canvas.FillColor = color;
        canvas.FillPath(path, WindingMode.EvenOdd);
    }

    static RectF ToRectF(Rect r) => new((float)r.X, (float)r.Y, (float)r.Width, (float)r.Height);
}
