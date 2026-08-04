namespace Immons.Tools.Maui.Inspector.Inspector.Panel;

/// <summary>Panel header: select/measure toggles, Tree/Props tabs, dump/refresh/close buttons.</summary>
internal sealed class PanelHeaderBar : Grid
{
    readonly Button _selectModeButton;
    readonly Button _measureModeButton;
    readonly Button _treeTabButton;
    readonly Button _propsTabButton;

    bool _selectMode = true;
    bool _measureMode;

    public event Action<bool>? SelectModeToggled;
    public event Action<bool>? MeasureModeToggled;
    public event Action<bool>? TabChanged;
    public event Action? DumpRequested;
    public event Action? RefreshRequested;
    public event Action? CloseRequested;
    public event Action? MoreRequested;

    /// <summary>The middle spacer — a convenient drag surface registered by the panel.</summary>
    public View DragSpacer { get; }

    public PanelHeaderBar()
    {
        _selectModeButton = Theme.MakeButton("⌖");
        _selectModeButton.FontSize = 16;
        _selectModeButton.Clicked += (_, _) => OnSelectClicked();

        _measureModeButton = Theme.MakeButton("↔︎");
        _measureModeButton.FontSize = 16;
        _measureModeButton.Clicked += (_, _) => OnMeasureClicked();

        _treeTabButton = Theme.MakeButton("☰︎ Tree");
        _treeTabButton.Clicked += (_, _) => TabChanged?.Invoke(true);
        _propsTabButton = Theme.MakeButton("▤︎ Props");
        _propsTabButton.Clicked += (_, _) => TabChanged?.Invoke(false);

        var dumpButton = Theme.MakeButton("📄 Dump");
        dumpButton.Clicked += (_, _) => DumpRequested?.Invoke();

        var moreButton = Theme.MakeButton("⋯");
        moreButton.FontSize = 16;
        moreButton.Clicked += (_, _) => MoreRequested?.Invoke();

        var refreshButton = Theme.MakeButton("↻");
        refreshButton.FontSize = 16;
        refreshButton.Clicked += (_, _) => RefreshRequested?.Invoke();

        var closeButton = Theme.MakeButton("✕");
        closeButton.Clicked += (_, _) => CloseRequested?.Invoke();

        BackgroundColor = Theme.PanelBg;
        ColumnDefinitions =
        [
            new ColumnDefinition(GridLength.Auto),
            new ColumnDefinition(GridLength.Auto),
            new ColumnDefinition(GridLength.Auto),
            new ColumnDefinition(GridLength.Auto),
            new ColumnDefinition(GridLength.Auto),
            new ColumnDefinition(GridLength.Star),
            new ColumnDefinition(GridLength.Auto),
            new ColumnDefinition(GridLength.Auto),
            new ColumnDefinition(GridLength.Auto),
        ];
        ColumnSpacing = 6;
        Padding = new Thickness(10, 4, 10, 6);
        this.NoSafeArea();

        this.Add(_selectModeButton, 0);
        this.Add(_measureModeButton, 1);
        this.Add(_treeTabButton, 2);
        this.Add(_propsTabButton, 3);
        this.Add(dumpButton, 4);
        this.Add(moreButton, 6);
        this.Add(refreshButton, 7);
        this.Add(closeButton, 8);

        // Opaque middle drag target (buttons keep their own hits).
        DragSpacer = new ContentView
        {
            BackgroundColor = Theme.PanelBg,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
        };
        this.Add(DragSpacer, 5);

        UpdateSelectModeVisual();
        UpdateMeasureModeVisual();
    }

    public void SetSelectModeVisual(bool on)
    {
        if (_measureMode && !on)
            return; // measure mode keeps hit-testing on
        _selectMode = on;
        UpdateSelectModeVisual();
    }

    public void SetMeasureModeVisual(bool on)
    {
        _measureMode = on;
        UpdateMeasureModeVisual();
    }

    public void SetTabVisual(bool treeActive)
    {
        _treeTabButton.BackgroundColor = treeActive ? Theme.Accent : Theme.PanelBg2;
        _treeTabButton.TextColor = treeActive ? Colors.White : Theme.TextPrimary;
        _propsTabButton.BackgroundColor = !treeActive ? Theme.Accent : Theme.PanelBg2;
        _propsTabButton.TextColor = !treeActive ? Colors.White : Theme.TextPrimary;
    }

    void OnSelectClicked()
    {
        if (_measureMode)
            return; // measure mode owns hit-testing
        _selectMode = !_selectMode;
        UpdateSelectModeVisual();
        SelectModeToggled?.Invoke(_selectMode);
    }

    void OnMeasureClicked()
    {
        _measureMode = !_measureMode;
        if (_measureMode && !_selectMode)
        {
            _selectMode = true;
            UpdateSelectModeVisual();
            SelectModeToggled?.Invoke(true);
        }
        UpdateMeasureModeVisual();
        MeasureModeToggled?.Invoke(_measureMode);
    }

    void UpdateSelectModeVisual()
    {
        _selectModeButton.BackgroundColor = _selectMode ? Theme.Accent : Theme.PanelBg2;
        _selectModeButton.TextColor = _selectMode ? Colors.White : Theme.TextPrimary;
        _selectModeButton.Opacity = _measureMode ? 0.55 : 1;
    }

    void UpdateMeasureModeVisual()
    {
        _measureModeButton.BackgroundColor = _measureMode ? Theme.MeasureAccent : Theme.PanelBg2;
        _measureModeButton.TextColor = _measureMode ? Colors.White : Theme.TextPrimary;
        UpdateSelectModeVisual();
    }
}
