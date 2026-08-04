namespace Immons.Tools.Maui.Inspector.Features.Measuring;

/// <summary>What a distance segment represents (drives badge label text).</summary>
internal enum DistanceKind
{
    /// <summary>Free space between facing edges on the horizontal axis.</summary>
    GapH,
    /// <summary>Free space between facing edges on the vertical axis.</summary>
    GapV,
    /// <summary>|Left(A) − Left(B)|</summary>
    AlignLeft,
    /// <summary>|Right(A) − Right(B)|</summary>
    AlignRight,
    /// <summary>|Top(A) − Top(B)|</summary>
    AlignTop,
    /// <summary>|Bottom(A) − Bottom(B)|</summary>
    AlignBottom,
}

/// <summary>A measured gap / edge offset between two elements, with a readable label in dp.</summary>
internal sealed record DistanceSegment(Point From, Point To, string Label, bool Horizontal, DistanceKind Kind);

/// <summary>
/// Figma-style spacing between two rectangles in the same coordinate space (dp).
/// Outer gaps when separated; edge alignment offsets when overlapping on an axis.
/// </summary>
internal static class DistanceMath
{
    const double Epsilon = 0.5;

    public static IReadOnlyList<DistanceSegment> Compute(Rect a, Rect b)
    {
        var segments = new List<DistanceSegment>(4);

        var overlapX = HorizontalOverlap(a, b);
        var overlapY = VerticalOverlap(a, b);

        if (overlapX is null && overlapY is null)
        {
            AddOuterHorizontal(segments, a, b);
            AddOuterVertical(segments, a, b);
        }
        else if (overlapX is null)
        {
            AddOuterHorizontal(segments, a, b);
            AddVerticalEdgeOffsets(segments, a, b);
        }
        else if (overlapY is null)
        {
            AddOuterVertical(segments, a, b);
            AddHorizontalEdgeOffsets(segments, a, b);
        }
        else
        {
            AddHorizontalEdgeOffsets(segments, a, b);
            AddVerticalEdgeOffsets(segments, a, b);
        }

        return segments;
    }

    static void AddOuterHorizontal(List<DistanceSegment> segments, Rect a, Rect b)
    {
        var y = AlignY(a, b);
        if (a.Right <= b.Left + Epsilon)
            AddSegment(segments, a.Right, y, b.Left, y, DistanceKind.GapH);
        else if (b.Right <= a.Left + Epsilon)
            AddSegment(segments, b.Right, y, a.Left, y, DistanceKind.GapH);
    }

    static void AddOuterVertical(List<DistanceSegment> segments, Rect a, Rect b)
    {
        var x = AlignX(a, b);
        if (a.Bottom <= b.Top + Epsilon)
            AddSegment(segments, x, a.Bottom, x, b.Top, DistanceKind.GapV);
        else if (b.Bottom <= a.Top + Epsilon)
            AddSegment(segments, x, b.Bottom, x, a.Top, DistanceKind.GapV);
    }

    static void AddHorizontalEdgeOffsets(List<DistanceSegment> segments, Rect a, Rect b)
    {
        var y = AlignY(a, b);
        AddEdgeDelta(segments, a.Left, b.Left, y, DistanceKind.AlignLeft);
        AddEdgeDelta(segments, a.Right, b.Right, y, DistanceKind.AlignRight);
    }

    static void AddVerticalEdgeOffsets(List<DistanceSegment> segments, Rect a, Rect b)
    {
        var x = AlignX(a, b);
        AddEdgeDelta(segments, a.Top, b.Top, x, DistanceKind.AlignTop);
        AddEdgeDelta(segments, a.Bottom, b.Bottom, x, DistanceKind.AlignBottom);
    }

    static void AddEdgeDelta(List<DistanceSegment> segments, double edgeA, double edgeB, double cross, DistanceKind kind)
    {
        if (Math.Abs(edgeA - edgeB) < Epsilon)
            return;

        var horizontal = kind is DistanceKind.AlignLeft or DistanceKind.AlignRight or DistanceKind.GapH;
        if (horizontal)
            AddSegment(segments, edgeA, cross, edgeB, cross, kind);
        else
            AddSegment(segments, cross, edgeA, cross, edgeB, kind);
    }

    static void AddSegment(List<DistanceSegment> segments, double x1, double y1, double x2, double y2, DistanceKind kind)
    {
        var horizontal = kind is DistanceKind.GapH or DistanceKind.AlignLeft or DistanceKind.AlignRight;
        var length = horizontal ? Math.Abs(x2 - x1) : Math.Abs(y2 - y1);
        if (length < Epsilon)
            return;

        segments.Add(new DistanceSegment(
            new Point(x1, y1),
            new Point(x2, y2),
            FormatLabel(kind, length),
            horizontal,
            kind));
    }

    static string FormatLabel(DistanceKind kind, double length)
    {
        var n = ValueFormatter.F(length);
        return kind switch
        {
            DistanceKind.GapH => $"←{n}→",
            DistanceKind.GapV => $"↑{n}↓",
            DistanceKind.AlignLeft => $"L {n}",
            DistanceKind.AlignRight => $"R {n}",
            DistanceKind.AlignTop => $"T {n}",
            DistanceKind.AlignBottom => $"B {n}",
            _ => n,
        };
    }

    static (double Top, double Bottom)? VerticalOverlap(Rect a, Rect b)
    {
        var top = Math.Max(a.Top, b.Top);
        var bottom = Math.Min(a.Bottom, b.Bottom);
        return bottom > top + Epsilon ? (top, bottom) : null;
    }

    static (double Left, double Right)? HorizontalOverlap(Rect a, Rect b)
    {
        var left = Math.Max(a.Left, b.Left);
        var right = Math.Min(a.Right, b.Right);
        return right > left + Epsilon ? (left, right) : null;
    }

    static double AlignY(Rect a, Rect b)
    {
        if (a.Bottom <= b.Top + Epsilon)
            return (a.Bottom + b.Top) / 2;
        if (b.Bottom <= a.Top + Epsilon)
            return (b.Bottom + a.Top) / 2;
        if (VerticalOverlap(a, b) is { } o)
            return (o.Top + o.Bottom) / 2;
        return (a.Center.Y + b.Center.Y) / 2;
    }

    static double AlignX(Rect a, Rect b)
    {
        if (a.Right <= b.Left + Epsilon)
            return (a.Right + b.Left) / 2;
        if (b.Right <= a.Left + Epsilon)
            return (b.Right + a.Left) / 2;
        if (HorizontalOverlap(a, b) is { } o)
            return (o.Left + o.Right) / 2;
        return (a.Center.X + b.Center.X) / 2;
    }
}
