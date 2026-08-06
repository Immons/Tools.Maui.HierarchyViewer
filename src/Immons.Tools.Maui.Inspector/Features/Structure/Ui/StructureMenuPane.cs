using Immons.Tools.Maui.Inspector.Inspector;

namespace Immons.Tools.Maui.Inspector.Features.Structure.Ui;

/// <summary>
/// On-device counterpart of the web context menu: structural actions for a tree element,
/// with a searchable catalog picker for Add/Wrap. Overlays the whole panel while open.
/// </summary>
internal sealed class StructureMenuPane : Grid
{
    /// <summary>Fired after a successful edit; the argument is the element to select (may be null).</summary>
    public event Action<VisualElement?>? Edited;

    readonly Label _title = Theme.MakeLabel(bold: true);
    readonly Label _error = Theme.MakeLabel(color: Colors.Orange, size: 12);
    readonly VerticalStackLayout _body = new() { Spacing = 4 };

    VisualElement? _target;

    public StructureMenuPane()
    {
        this.NoSafeArea();
        IsVisible = false;

        var backdrop = new BoxView { Color = Color.FromArgb("#88000000") };
        var closeTap = new TapGestureRecognizer();
        closeTap.Tapped += (_, _) => Hide();
        backdrop.GestureRecognizers.Add(closeTap);
        Add(backdrop);

        _error.LineBreakMode = LineBreakMode.WordWrap;

        var card = new Border
        {
            Background = new SolidColorBrush(Theme.PanelBg2),
            Stroke = new SolidColorBrush(Theme.Divider),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(10) },
            Padding = new Thickness(12),
            WidthRequest = 280,
            MaximumHeightRequest = 380,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children = { _title, _error, new ScrollView { Content = _body } },
            },
        };
        // Swallow taps on the card so only the backdrop closes the menu.
        card.GestureRecognizers.Add(new TapGestureRecognizer());
        Add(card);
    }

    public void Show(VisualElement target)
    {
        _target = target;
        _error.Text = "";
        _title.Text = target.GetType().Name;
        BuildActions();
        IsVisible = true;
    }

    void Hide() => IsVisible = false;

    void BuildActions()
    {
        _body.Clear();
        AddAction("＋ Add element…", () => ShowPicker(containersOnly: false, wrap: false));
        AddAction("▣ Wrap in…", () => ShowPicker(containersOnly: true, wrap: true));
        AddAction("⬚ Unwrap", () => RunSimple(id => InspectorServices.Current.Structure.UnwrapElement(id), selectTarget: false));
        AddAction("↑ Move up", () => RunSimple(id => InspectorServices.Current.Structure.Move(id, -1)));
        AddAction("↓ Move down", () => RunSimple(id => InspectorServices.Current.Structure.Move(id, 1)));
        AddAction("✕ Remove", () => RunSimple(id => InspectorServices.Current.Structure.Remove(id), selectTarget: false));
        AddAction("Cancel", Hide);
    }

    void AddAction(string text, Action onTapped)
    {
        var button = Theme.MakeButton(text);
        button.HorizontalOptions = LayoutOptions.Fill;
        button.Clicked += (_, _) => onTapped();
        _body.Add(button);
    }

    void ShowPicker(bool containersOnly, bool wrap)
    {
        _body.Clear();
        _error.Text = "";

        var filter = new Entry
        {
            Placeholder = containersOnly ? "Search containers…" : "Search controls…",
            TextColor = Theme.TextPrimary,
            PlaceholderColor = Theme.TextSecondary,
            FontSize = Theme.FontSize,
        };
        var list = new VerticalStackLayout { Spacing = 2 };

        void Render()
        {
            list.Clear();
            var query = filter.Text?.Trim() ?? "";
            foreach (var entry in InspectorServices.Current.Catalog.All())
            {
                if (containersOnly && !entry.IsContainer)
                    continue;
                if (query.Length > 0 && !entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    continue;

                var row = new VerticalStackLayout { Padding = new Thickness(6, 4) };
                row.Add(Theme.MakeLabel(entry.Name, bold: true));
                row.Add(Theme.MakeLabel(entry.Description, color: Theme.TextSecondary, size: 11));
                var tap = new TapGestureRecognizer();
                var typeName = entry.TypeName;
                tap.Tapped += (_, _) =>
                {
                    if (wrap)
                        RunReturningId(id => InspectorServices.Current.Structure.Wrap(id, typeName));
                    else
                        RunReturningId(id => InspectorServices.Current.Structure.Add(id, typeName));
                };
                row.GestureRecognizers.Add(tap);
                list.Add(row);
            }
        }

        filter.TextChanged += (_, _) => Render();
        Render();

        _body.Add(filter);
        _body.Add(list);
        AddAction("Back", BuildActions);
    }

    void RunSimple(Func<int, string?> op, bool selectTarget = true)
    {
        if (_target is not { } target)
            return;
        var error = op(InspectorServices.Current.Elements.GetId(target));
        if (error != null)
        {
            _error.Text = error;
            return;
        }
        Hide();
        Edited?.Invoke(selectTarget ? target : null);
    }

    void RunReturningId(Func<int, (int Id, string? Error)> op)
    {
        if (_target is not { } target)
            return;
        var (id, error) = op(InspectorServices.Current.Elements.GetId(target));
        if (error != null)
        {
            _error.Text = error;
            return;
        }
        Hide();
        Edited?.Invoke(InspectorServices.Current.Elements.Find(id));
    }
}
