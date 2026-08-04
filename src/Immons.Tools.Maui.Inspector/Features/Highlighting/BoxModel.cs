namespace Immons.Tools.Maui.Inspector.Features.Highlighting;

/// <summary>Selected element geometry, in highlight-layer coordinates (dp).</summary>
internal sealed record BoxModel(
    Rect MarginRect,
    Rect BoundsRect,
    Rect ContentRect,
    string Dimensions,
    Rect? CompareBounds = null,
    IReadOnlyList<DistanceSegment>? Distances = null,
    IReadOnlyList<(Rect Rect, int Depth)>? Guides = null);
