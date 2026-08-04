

namespace Immons.Tools.Maui.Inspector.Inspector;

/// <summary>
/// Per-window inspector: owns the activation gesture, the highlight layer and the bottom panel.
/// Platform pieces live in the .android/.apple/.default partials.
/// </summary>
internal sealed partial class WindowInspector
{
    readonly Window _window;
    readonly MauiInspectorOptions _options;

    IMauiContext? _mauiContext;
    HighlightLayer? _highlightLayer;
    PanelLayer? _panelLayer;
    VisualElement? _selected;
    VisualElement? _compare;
    bool _measureMode;

    public bool IsShown { get; private set; }

    public WindowInspector(Window window, MauiInspectorOptions options)
    {
        _window = window;
        _options = options;
    }

    /// <summary>Called every time the window (re)connects to a platform handler.</summary>
    public void OnHandlerChanged()
    {
        Hide();
        DetachPlatform();
        _mauiContext = _window.Handler?.MauiContext;
        if (_mauiContext != null && _options.Activation == InspectorActivation.LongPress)
            AttachPlatform();
    }

    public void Detach() => DetachPlatform();

    public void Show(Point? windowPoint)
    {
        _mauiContext ??= _window.Handler?.MauiContext;
        if (_mauiContext == null)
            return;

        // Upgrade from remote highlight-only mode to the full overlay.
        if (IsShown && _panelLayer == null)
            Hide();

        if (!IsShown)
        {
            BuildLayers();
            AddLayersPlatform();
            IsShown = true;
            SetSelectMode(true);
            RefreshTree();
        }

        if (windowPoint is { } p)
            SelectAt(p);
        else if (_selected == null && RootElements().LastOrDefault() is { } root)
            SelectElement(root);
    }

    public void Hide()
    {
        if (!IsShown)
            return;

        RemoveLayersPlatform();
        _highlightLayer?.Handler?.DisconnectHandler();
        _panelLayer?.Handler?.DisconnectHandler();
        _highlightLayer = null;
        _panelLayer = null;
        _selected = null;
        _compare = null;
        _measureMode = false;
        IsShown = false;
    }

    void BuildLayers()
    {
        _highlightLayer = new HighlightLayer();
        _highlightLayer.Tapped += p => SelectAt(new Point(p.X + LayerOrigin.X, p.Y + LayerOrigin.Y));

        _panelLayer = new PanelLayer();
        _panelLayer.CloseRequested += Hide;
        _panelLayer.RefreshRequested += () =>
        {
            RefreshTree();
            if (_selected != null)
            {
                // Keep compare if still valid; just re-measure.
                UpdateHighlight();
                if (!_measureMode || _compare == null)
                    SelectElement(_selected);
                else
                {
                    var sections = InspectorServices.Properties.Collect(_selected, GetRectInWindow(_selected));
                    _panelLayer.ShowSelection(_selected, sections, ParentChain(_selected), scrollTree: false);
                }
            }
        };
        _panelLayer.SelectModeToggled += SetSelectMode;
        _panelLayer.MeasureModeToggled += SetMeasureMode;
        _panelLayer.DebugPaintToggled += SetDebugPaint;
        _panelLayer.ElementPicked += (el, scrollTree) =>
        {
            if (_measureMode && _selected != null)
                SetCompareElement(el);
            else
                SelectElement(el, scrollTree);
        };
        _panelLayer.DumpRequested += DumpHierarchy;
        _panelLayer.PropertyEdited += OnPropertyEdited;
        _panelLayer.StructureEdited += () =>
        {
            if (_selected is not { } element || _panelLayer == null)
                return;
            OnPropertyEdited();
            // Rebuild the sections (span/definition counts changed) keeping the scroll position.
            var sections = InspectorServices.Properties.Collect(element, GetRectInWindow(element));
            _panelLayer.ShowSelection(element, sections, ParentChain(element), scrollTree: false, preservePropsScroll: true);
        };
        _panelLayer.ToolsDispatcher = _window.Dispatcher;
        _panelLayer.BottomInset = GetBottomInsetPlatform();
        _panelLayer.WindowSizeProvider = () => new Size(_window.Width, _window.Height);
        // Panel is parented on UIWindow/DecorView with a fixed native frame — MAUI
        // TranslationX/Y alone often does nothing there; platform applies the transform.
        _panelLayer.ApplyDragOffset = SetPanelOffsetPlatform;
    }

    Point LayerOrigin => GetLayerOriginPlatform();

    /// <summary>Roots in bottom-to-top order: the window page followed by any modal pages.</summary>
    IEnumerable<VisualElement> RootElements()
    {
        if (_window.Page is { } page)
        {
            yield return page;

            IReadOnlyList<Page>? modals = null;
            try { modals = page.Navigation?.ModalStack; }
            catch { /* navigation may be unavailable mid-teardown */ }

            if (modals != null)
                foreach (var modal in modals)
                    yield return modal;
        }
    }

    void RefreshTree() => _panelLayer?.SetTree(TreeNode.Build(RootElements()));

    /// <summary>Writes an indented, designer-oriented dump of the whole visual tree to the console.</summary>
    void DumpHierarchy()
    {
        var dump = HierarchyDumper.Dump(RootElements(), GetRectInWindow, new Size(_window.Width, _window.Height));
        foreach (var line in dump.Split('\n'))
            Console.WriteLine(line.TrimEnd('\r'));
    }

    /// <summary>Re-measures the highlight after a live property edit (twice: now and post-layout).</summary>
    void OnPropertyEdited()
    {
        if (_selected is not { } element)
            return;

        UpdateHighlight();
        _window.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(300), () =>
        {
            if (IsShown && ReferenceEquals(_selected, element))
                UpdateHighlight();
        });
    }

    Rect? GetRectInWindow(VisualElement element) => GetRectInWindowPlatform(element);

    static List<VisualElement> ParentChain(VisualElement element)
    {
        var chain = new List<VisualElement>();
        for (Element? current = element; current != null; current = current.Parent)
        {
            // Skip non-visual intermediaries (ShellContent etc.) but keep walking up.
            if (current is VisualElement ve)
                chain.Add(ve);
        }
        chain.Reverse();
        return chain;
    }

    /// <summary>Entry point from the platform long-press detectors; point is in window coordinates (dp).</summary>
    internal void OnLongPressDetected(Point windowPoint)
    {
        if (IsShown && _panelLayer != null)
            return;
        _window.Dispatcher.Dispatch(() => Show(windowPoint));
    }

    // Platform pieces:
    private partial void AttachPlatform();
    private partial void DetachPlatform();
    private partial void AddLayersPlatform();
    private partial void RemoveLayersPlatform();
    private partial void SetPanelOffsetPlatform(double xDp, double yDp);
    private partial Rect? GetRectInWindowPlatform(VisualElement element);
    private partial Point GetLayerOriginPlatform();
    private partial double GetBottomInsetPlatform();
}
