namespace Immons.Tools.Maui.Inspector.Features.Structure;

/// <summary>Puts children into parents (and takes them back out) per container kind.</summary>
internal static class ElementAttacher
{
    /// <summary>Null on success, otherwise the reason the parent cannot take the child.</summary>
    public static string? Attach(VisualElement parent, View child, int index = -1)
    {
        switch (parent)
        {
            case Layout layout:
                if (index >= 0 && index <= layout.Children.Count)
                    layout.Children.Insert(index, child);
                else
                    layout.Children.Add(child);
                return null;

            case Border border:
                if (border.Content != null)
                    return "Border already has content — remove it first";
                border.Content = child;
                return null;

            case ScrollView scroll:
                if (scroll.Content != null)
                    return "ScrollView already has content — remove it first";
                scroll.Content = child;
                return null;

            case ContentView view:
                if (view.Content != null)
                    return $"{parent.GetType().Name} already has content — remove it first";
                view.Content = child;
                return null;

            case ContentPage page:
                if (page.Content != null)
                    return "the page already has content — remove it first";
                page.Content = child;
                return null;

            default:
                return $"{parent.GetType().Name} does not accept children here";
        }
    }

    /// <summary>Detaches the element; returns its previous index (layouts) or 0, -1 when not found.</summary>
    public static int Detach(VisualElement element)
    {
        switch (element.Parent)
        {
            case Layout layout when element is IView view:
                var index = layout.Children.IndexOf(view);
                if (index >= 0)
                    layout.Children.RemoveAt(index);
                return index;

            case Border border when ReferenceEquals(border.Content, element):
                border.Content = null;
                return 0;

            case ScrollView scroll when ReferenceEquals(scroll.Content, element):
                scroll.Content = null;
                return 0;

            case ContentView view when ReferenceEquals(view.Content, element):
                view.Content = null;
                return 0;

            case ContentPage page when ReferenceEquals(page.Content, element):
                page.Content = null;
                return 0;

            default:
                return -1;
        }
    }
}
