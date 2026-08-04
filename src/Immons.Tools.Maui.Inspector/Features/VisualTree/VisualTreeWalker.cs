namespace Immons.Tools.Maui.Inspector.Features.VisualTree;

internal static class VisualTreeWalker
{
    /// <summary>
    /// Returns the visual children of an element, flattening through non-visual intermediaries
    /// (e.g. ShellItem → ShellSection → ShellContent between a Shell and its pages).
    /// Some layouts (Compatibility.*, custom containers, BindableLayout hosts) return an empty
    /// visual-tree list even though they do have children — fall back to ILayout / IContentView.
    /// </summary>
    public static IEnumerable<VisualElement> GetVisualChildren(IVisualTreeElement element)
    {
        var any = false;
        foreach (var child in element.GetVisualChildren())
        {
            if (child is VisualElement ve)
            {
                any = true;
                yield return ve;
            }
            else
            {
                foreach (var nested in GetVisualChildren(child))
                {
                    any = true;
                    yield return nested;
                }
            }
        }

        if (any)
            yield break;

        if (element is Microsoft.Maui.ILayout layout)
        {
            foreach (var view in layout)
            {
                if (view is VisualElement ve)
                    yield return ve;
            }
        }
        else if (element is IContentView { Content: VisualElement content })
        {
            yield return content;
        }
    }
}
