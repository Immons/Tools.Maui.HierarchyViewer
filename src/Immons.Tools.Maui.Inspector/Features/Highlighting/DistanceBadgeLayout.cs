using Microsoft.Maui.Graphics;

namespace Immons.Tools.Maui.Inspector.Features.Highlighting;

/// <summary>Placement of measure-mode badges: midpoint anchoring, overlap nudging, clamping.</summary>
internal static class DistanceBadgeLayout
{
    /// <summary>Label sits on the segment midpoint: above for H, left for V (flip if clipped).</summary>
    public static RectF Preferred(DistanceSegment seg, RectF dirtyRect, float w, float h)
    {
        var midX = (float)((seg.From.X + seg.To.X) / 2);
        var midY = (float)((seg.From.Y + seg.To.Y) / 2);
        const float gap = 6f;

        float x, y;
        if (seg.Horizontal)
        {
            x = midX - w / 2;
            y = midY - h - gap;
            if (y < dirtyRect.Top + 4)
                y = midY + gap;
        }
        else
        {
            x = midX - w - gap;
            y = midY - h / 2;
            if (x < dirtyRect.Left + 4)
                x = midX + gap;
        }

        return Clamp(x, y, w, h, dirtyRect);
    }

    /// <summary>Minimal separation if two badges actually overlap — push along the shorter axis only.</summary>
    public static void NudgeOverlaps(List<(RectF Rect, string Text)> badges, RectF dirtyRect)
    {
        const float pad = 4f;
        for (var pass = 0; pass < 3; pass++)
        {
            var moved = false;
            for (var i = 0; i < badges.Count; i++)
            for (var j = i + 1; j < badges.Count; j++)
            {
                var a = badges[i].Rect;
                var b = badges[j].Rect;
                if (!Overlap(a, b, pad))
                    continue;

                var ox = Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left) + pad;
                var oy = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top) + pad;

                if (ox <= oy)
                {
                    var dx = ox / 2 + 1;
                    var leftFirst = a.Center.X <= b.Center.X;
                    a = OffsetClamped(a, leftFirst ? -dx : dx, 0, dirtyRect);
                    b = OffsetClamped(b, leftFirst ? dx : -dx, 0, dirtyRect);
                }
                else
                {
                    var dy = oy / 2 + 1;
                    var topFirst = a.Center.Y <= b.Center.Y;
                    a = OffsetClamped(a, 0, topFirst ? -dy : dy, dirtyRect);
                    b = OffsetClamped(b, 0, topFirst ? dy : -dy, dirtyRect);
                }

                badges[i] = (a, badges[i].Text);
                badges[j] = (b, badges[j].Text);
                moved = true;
            }
            if (!moved)
                break;
        }
    }

    public static RectF Clamp(float x, float y, float w, float h, RectF dirtyRect)
    {
        x = Math.Clamp(x, dirtyRect.Left + 4, Math.Max(dirtyRect.Left + 4, dirtyRect.Right - w - 4));
        y = Math.Clamp(y, dirtyRect.Top + 4, Math.Max(dirtyRect.Top + 4, dirtyRect.Bottom - h - 4));
        return new RectF(x, y, w, h);
    }

    static bool Overlap(RectF a, RectF b, float pad) =>
        a.Left < b.Right + pad && a.Right + pad > b.Left &&
        a.Top < b.Bottom + pad && a.Bottom + pad > b.Top;

    static RectF OffsetClamped(RectF r, float dx, float dy, RectF dirtyRect) =>
        Clamp(r.X + dx, r.Y + dy, r.Width, r.Height, dirtyRect);
}
