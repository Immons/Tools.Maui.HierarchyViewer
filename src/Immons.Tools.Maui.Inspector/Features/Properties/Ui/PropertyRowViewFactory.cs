namespace Immons.Tools.Maui.Inspector.Features.Properties.Ui;

/// <summary>Builds the value-side view of a property row: action link, group toggle or live editor.</summary>
internal sealed class PropertyRowViewFactory(
    Func<VisualElement?> elementProvider,
    Action edited,
    Action structureChanged,
    Func<string, bool> isGroupExpanded,
    Action<string> toggleGroup)
{
    public View Create(PropertyRow row, string sectionTitle)
    {
        if (row.Action is { } action)
            return ActionLink(row, sectionTitle, action);

        if (row.TogglesGroup is { } group)
            return GroupToggle(row, group);

        if (row.Editor is not { } editor)
            return Decorate(WithSwatch(ReadOnlyLabel(row.Value), row.Swatch), row, sectionTitle);

        var value = editor.Kind switch
        {
            EditorKind.Bool => BoolSwitch(row, sectionTitle, editor),
            EditorKind.Enum or EditorKind.LayoutOptions => ChoicePicker(row, sectionTitle, editor),
            _ => WithSwatch(TextEntry(row, sectionTitle, editor), row.Swatch),
        };
        return Decorate(value, row, sectionTitle);
    }

    /// <summary>Adds the web panel's extras to a row: ⋔ per-device editor, ✕ clear, binding/expression badges.</summary>
    View Decorate(View value, PropertyRow row, string sectionTitle)
    {
        var hasExtras = row.Editor != null || row.Binding != null || row.DeviceExpression != null;
        if (!hasExtras)
            return value;

        var stack = new VerticalStackLayout { Spacing = 2 }.NoSafeArea();

        var line = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
            ],
            ColumnSpacing = 2,
        }.NoSafeArea();
        line.Add(value, 0);

        if (row.Editor is { } editor)
        {
            line.Add(IconButton("⋔︎", "per platform / idiom", () => ToggleDeviceEditor(stack, row, sectionTitle, editor)), 1);
            if (editor.CanClear)
                line.Add(IconButton("✕", "clear", () => ClearValue(row, sectionTitle, editor)), 2);
        }

        stack.Add(line);

        if (row.DeviceExpression is { } expr)
            stack.Add(Badge("⋔︎ " + expr));
        if (row.Binding is { } binding)
            stack.Add(Badge("⛓︎ " + binding));

        return stack;
    }

    static Label Badge(string text)
    {
        var label = Theme.MakeLabel(text, Theme.MeasureAccent, Theme.FontSizeSmall);
        label.LineBreakMode = LineBreakMode.TailTruncation;
        return label;
    }

    static View IconButton(string glyph, string hint, Action onTap)
    {
        var label = Theme.MakeLabel(glyph, Theme.TextSecondary, Theme.FontSize);
        label.Padding = new Thickness(6, 4);
        label.VerticalOptions = LayoutOptions.Center;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => onTap();
        label.GestureRecognizers.Add(tap);
        return label;
    }

    void ClearValue(PropertyRow row, string sectionTitle, PropertyEditor editor)
    {
        if (!editor.Clear())
            return;
        InspectorServices.Current.History.Record(elementProvider(), sectionTitle, row.Name, row.Value, "(cleared)");
        structureChanged();
    }

    /// <summary>Inline OnPlatform/OnIdiom composer — the same idea as the web panel's ⋔ editor.</summary>
    void ToggleDeviceEditor(VerticalStackLayout host, PropertyRow row, string sectionTitle, PropertyEditor editor)
    {
        const string EditorId = "deviceEditor";
        if (host.Children.FirstOrDefault(c => c is View { StyleId: EditorId }) is View existing)
        {
            host.Remove(existing);
            return;
        }

        var mode = new Picker
        {
            ItemsSource = new List<string> { "default", "OnPlatform", "OnIdiom" },
            SelectedIndex = 0,
            TextColor = Theme.TextPrimary,
            BackgroundColor = Theme.PanelBg2,
            FontSize = Theme.FontSizeSmall,
        };

        var fields = new VerticalStackLayout { Spacing = 2 }.NoSafeArea();
        var entries = new Dictionary<string, Entry>();

        void Render()
        {
            fields.Clear();
            entries.Clear();
            var keys = mode.SelectedIndex switch
            {
                1 => new[] { "Default", "iOS", "Android", "WinUI" },
                2 => ["Default", "Phone", "Tablet", "Desktop"],
                _ => ["Value"],
            };
            foreach (var key in keys)
            {
                var entry = new Entry
                {
                    Text = key is "Value" or "Default" ? row.Value : "",
                    Placeholder = key,
                    TextColor = Theme.TextPrimary,
                    PlaceholderColor = Theme.TextSecondary,
                    BackgroundColor = Theme.PanelBg2,
                    FontSize = Theme.FontSizeSmall,
                    FontFamily = Theme.MonoFont,
                    HeightRequest = 30,
                };
                entries[key] = entry;
                fields.Add(entry);
            }
        }

        mode.SelectedIndexChanged += (_, _) => Render();
        Render();

        var apply = Theme.MakeLabel("✓ Apply", Theme.Accent, Theme.FontSize, bold: true);
        apply.Padding = new Thickness(0, 6);
        var applyTap = new TapGestureRecognizer();
        applyTap.Tapped += (_, _) =>
        {
            var text = Compose(mode.SelectedIndex, entries);
            if (text == null || !editor.Apply(text))
                return;
            InspectorServices.Current.History.Record(elementProvider(), sectionTitle, row.Name, row.Value, text);
            structureChanged();
        };
        apply.GestureRecognizers.Add(applyTap);

        var box = new VerticalStackLayout
        {
            StyleId = EditorId,
            Spacing = 3,
            Padding = new Thickness(6),
            BackgroundColor = Theme.PanelBg2,
            Children = { mode, fields, apply },
        }.NoSafeArea();
        host.Add(box);
    }

    static string? Compose(int modeIndex, Dictionary<string, Entry> entries)
    {
        if (modeIndex == 0)
            return entries.TryGetValue("Value", out var single) ? single.Text ?? "" : null;

        var keyword = modeIndex == 1 ? "OnPlatform" : "OnIdiom";
        var parts = entries
            .Where(e => !string.IsNullOrWhiteSpace(e.Value.Text))
            .Select(e => $"{e.Key}={Quote(e.Value.Text!.Trim())}")
            .ToList();
        return parts.Count == 0 ? null : $"{{{keyword} {string.Join(", ", parts)}}}";
    }

    static string Quote(string value) =>
        value.StartsWith('{') || !(value.Contains(',') || value.Contains(' ')) ? value : $"'{value}'";

    View ActionLink(PropertyRow row, string sectionTitle, Action action)
    {
        var link = Theme.MakeLabel(row.Value, Theme.Accent, Theme.FontSize, bold: true);
        link.Padding = new Thickness(0, 6);
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            try { action(); }
            catch { /* structural edit failed — panel refresh below shows actual state */ }
            InspectorServices.Current.History.Record(elementProvider(), sectionTitle, row.Value, "", "(action)", canUndo: false);
            structureChanged();
        };
        link.GestureRecognizers.Add(tap);
        return link;
    }

    View GroupToggle(PropertyRow row, string group)
    {
        var label = Theme.MakeLabel("", Theme.Accent, Theme.FontSize, bold: true);
        label.Padding = new Thickness(0, 4);
        void UpdateText() => label.Text = $"{row.Value} {(isGroupExpanded(group) ? "▾" : "▸")}";
        UpdateText();
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            toggleGroup(group);
            UpdateText();
        };
        label.GestureRecognizers.Add(tap);
        return label;
    }

    View BoolSwitch(PropertyRow row, string sectionTitle, PropertyEditor editor)
    {
        var sw = new Switch
        {
            IsToggled = bool.TryParse(row.Value, out var b) && b,
            OnColor = Theme.Accent,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center,
            Scale = 0.8,
            AnchorX = 0,
        };
        sw.Toggled += (_, e) =>
        {
            if (editor.Apply(e.Value.ToString()))
            {
                InspectorServices.Current.History.Record(elementProvider(), sectionTitle, row.Name, (!e.Value).ToString(), e.Value.ToString());
                edited();
            }
        };
        return sw;
    }

    View ChoicePicker(PropertyRow row, string sectionTitle, PropertyEditor editor)
    {
        var picker = new Picker
        {
            ItemsSource = editor.Choices!.ToList(),
            TextColor = Theme.TextPrimary,
            TitleColor = Theme.TextSecondary,
            BackgroundColor = Theme.PanelBg2,
            FontSize = Theme.FontSize,
            HorizontalOptions = LayoutOptions.Start,
            MinimumWidthRequest = 120,
        };
        var lastApplied = row.Value;
        var index = editor.Choices!.ToList().IndexOf(row.Value);
        if (index >= 0)
            picker.SelectedIndex = index;
        picker.SelectedIndexChanged += (_, _) =>
        {
            if (picker.SelectedItem is string choice && choice != lastApplied && editor.Apply(choice))
            {
                InspectorServices.Current.History.Record(elementProvider(), sectionTitle, row.Name, lastApplied, choice);
                lastApplied = choice;
                edited();
            }
        };
        return picker;
    }

    View TextEntry(PropertyRow row, string sectionTitle, PropertyEditor editor)
    {
        var entry = new Entry
        {
            Text = row.Value,
            TextColor = Theme.TextPrimary,
            PlaceholderColor = Theme.TextSecondary,
            BackgroundColor = Theme.PanelBg2,
            FontSize = Theme.FontSize,
            FontFamily = Theme.MonoFont,
            HeightRequest = 32,
            MinimumWidthRequest = 90,
            ReturnType = ReturnType.Done,
            VerticalOptions = LayoutOptions.Center,
        };

        var lastApplied = row.Value;
        void TryApply(bool revertOnFail)
        {
            var text = entry.Text ?? "";
            if (text == lastApplied)
                return;
            if (editor.Apply(text))
            {
                InspectorServices.Current.History.Record(elementProvider(), sectionTitle, row.Name, lastApplied, text);
                lastApplied = text;
                edited();
            }
            else if (revertOnFail)
            {
                entry.Text = lastApplied;
            }
        }

        // Live apply while typing (invalid intermediate input is simply ignored);
        // commit with revert-on-fail when the field is left. This also makes edits
        // work on iOS where neither Enter nor tapping elsewhere may unfocus the field.
        entry.TextChanged += (_, _) => TryApply(revertOnFail: false);
        entry.Completed += (_, _) => TryApply(revertOnFail: true);
        entry.Unfocused += (_, _) => TryApply(revertOnFail: true);

        return entry;
    }

    static Label ReadOnlyLabel(string value) => new()
    {
        Text = value,
        TextColor = Theme.TextPrimary,
        FontSize = Theme.FontSize,
        FontFamily = Theme.MonoFont,
        LineBreakMode = LineBreakMode.CharacterWrap,
        VerticalOptions = LayoutOptions.Center,
    };

    static View WithSwatch(View value, Color? swatchColor)
    {
        if (swatchColor == null)
            return value;

        var swatch = new Border
        {
            WidthRequest = 12,
            HeightRequest = 12,
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(Theme.Divider),
            Background = new SolidColorBrush(swatchColor),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(3) },
            VerticalOptions = LayoutOptions.Center,
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
            ],
            ColumnSpacing = 6,
        }.NoSafeArea();
        grid.Add(swatch, 0);
        grid.Add(value, 1);
        return grid;
    }
}
