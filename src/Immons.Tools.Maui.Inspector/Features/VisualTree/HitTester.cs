namespace Immons.Tools.Maui.Inspector.Features.VisualTree;

internal static class HitTester
{
    /// <summary>
    /// Returns the deepest visible element whose window-space bounds contain the point,
    /// walking children in reverse paint order (highest ZIndex / last sibling first).
    /// </summary>
    public static VisualElement? HitTest(
        IEnumerable<VisualElement> rootsBottomToTop,
        Point point,
        Func<VisualElement, Rect?> bounds)
    {
        foreach (var root in rootsBottomToTop.Reverse())
        {
            if (Hit(root, point, bounds) is { } hit)
                return hit;
        }
        return null;
    }

    static VisualElement? Hit(VisualElement element, Point point, Func<VisualElement, Rect?> bounds)
    {
        if (!element.IsVisible)
            return null;

        var children = VisualTreeWalker.GetVisualChildren(element)
            .Select((child, index) => (Child: child, Index: index))
            .OrderByDescending(t => t.Child.ZIndex)
            .ThenByDescending(t => t.Index);

        foreach (var (child, _) in children)
        {
            if (Hit(child, point, bounds) is { } hit)
                return hit;
        }

        return bounds(element) is { } rect && rect.Contains(point) ? element : null;
    }
}
