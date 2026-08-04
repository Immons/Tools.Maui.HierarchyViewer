namespace Immons.Tools.Maui.Inspector.Inspector.Panel;

/// <summary>Horizontal parent-chain bar; tapping an ancestor selects it.</summary>
internal sealed class BreadcrumbBar : ScrollView
{
    readonly HorizontalStackLayout _items;

    public event Action<VisualElement>? Picked;

    public BreadcrumbBar()
    {
        _items = new HorizontalStackLayout { Spacing = 2, Padding = new Thickness(12, 0, 12, 6) }.NoSafeArea();
        Orientation = ScrollOrientation.Horizontal;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Never;
        BackgroundColor = Theme.PanelBg;
        Content = _items;
    }

    public void Update(List<VisualElement> chain)
    {
        _items.Clear();

        for (var i = 0; i < chain.Count; i++)
        {
            var element = chain[i];
            var isLast = i == chain.Count - 1;

            var label = Theme.MakeLabel(
                element.GetType().Name,
                isLast ? Theme.Accent : Theme.TextSecondary,
                Theme.FontSizeSmall,
                bold: isLast);
            label.Padding = new Thickness(2, 4);

            if (!isLast)
            {
                var tap = new TapGestureRecognizer();
                var captured = element;
                tap.Tapped += (_, _) => Picked?.Invoke(captured);
                label.GestureRecognizers.Add(tap);
            }

            _items.Add(label);

            if (!isLast)
                _items.Add(Theme.MakeLabel("›", Theme.TextSecondary, Theme.FontSizeSmall));
        }
    }
}
