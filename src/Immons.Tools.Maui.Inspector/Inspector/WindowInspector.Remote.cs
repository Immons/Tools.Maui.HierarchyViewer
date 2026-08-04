namespace Immons.Tools.Maui.Inspector.Inspector;

/// <summary>Remote (web server) access surface of the inspector — see IWindowInspector.</summary>
internal sealed partial class WindowInspector : IWindowInspector
{
    public IDispatcher Dispatcher => _window.Dispatcher;

    public IMauiContext? MauiContext => _mauiContext;

    public VisualElement? SelectedElement => _selected;

    public VisualElement? CompareElement => _compare;

    public bool MeasureMode => _measureMode;

    public IEnumerable<VisualElement> Roots => RootElements();

    public Rect? BoundsOf(VisualElement element) => GetRectInWindow(element);

    public Size WindowSize => new(_window.Width, _window.Height);

    public string BuildDump() =>
        HierarchyDumper.Dump(RootElements(), GetRectInWindow, new Size(_window.Width, _window.Height));

    /// <summary>Hit-test at window coordinates, driven by the web mirror click.</summary>
    public bool RemoteSelectAt(Point windowPoint)
    {
        var hit = HitTester.HitTest(RootElements(), windowPoint, GetRectInWindow);
        if (hit == null)
            return false;
        if (_measureMode && _selected != null)
            SetCompareElement(hit);
        else
            RemoteSelect(hit);
        return true;
    }

    /// <summary>Selection driven from the web client: highlights on the device without opening the panel.</summary>
    public void RemoteSelect(VisualElement element)
    {
        if (IsShown && _panelLayer != null)
        {
            _compare = null;
            SelectElement(element);
            return;
        }

        if (!EnsureRemoteOverlay())
            return;

        _selected = element;
        _compare = null;
        UpdateHighlight();
    }

    /// <summary>Measure driven from the web client: primary + optional compare target.</summary>
    public void RemoteMeasure(VisualElement primary, VisualElement? compare)
    {
        if (!IsShown && !EnsureRemoteOverlay())
            return;

        _selected = primary;
        _compare = compare != null && !ReferenceEquals(compare, primary) ? compare : null;
        UpdateHighlight();
    }

    bool EnsureRemoteOverlay()
    {
        if (IsShown)
            return true;

        _mauiContext ??= _window.Handler?.MauiContext;
        if (_mauiContext == null)
            return false;

        _highlightLayer = new HighlightLayer();
        _highlightLayer.Tapped += p => SelectAt(new Point(p.X + LayerOrigin.X, p.Y + LayerOrigin.Y));
        AddLayersPlatform();
        IsShown = true;
        return true;
    }

    /// <summary>
    /// Measure mode driven from the web client. Shared with the on-device panel; in remote
    /// highlight-only mode the overlay starts catching taps so the compare target can be
    /// picked directly on the device.
    /// </summary>
    public void SetRemoteMeasureMode(bool on)
    {
        if (_panelLayer != null)
        {
            SetMeasureMode(on);
            return;
        }

        if (on && !EnsureRemoteOverlay())
            return;

        _measureMode = on;
        if (!on)
        {
            _compare = null;
            UpdateHighlight();
        }
        _highlightLayer?.SetSelectMode(_measureMode || _remoteSelectMode);
    }

    bool _remoteSelectMode;

    public bool RemoteSelectModeActive => _remoteSelectMode;

    public bool OverlayShown => IsShown && _panelLayer != null;

    /// <summary>Web-driven pick mode: single taps on the device select elements without the panel.</summary>
    public void SetRemoteSelectMode(bool on)
    {
        _remoteSelectMode = on;

        if (_panelLayer != null)
        {
            SetSelectMode(on || _measureMode);
            return;
        }

        if (on && !EnsureRemoteOverlay())
            return;

        _highlightLayer?.SetSelectMode(_remoteSelectMode || _measureMode);
    }

    /// <summary>Web-driven toggle of the full on-device panel; selection and modes survive.</summary>
    public void SetOverlayShown(bool on)
    {
        if (on == OverlayShown)
            return;

        var selected = _selected;
        var measure = _measureMode;

        if (on)
        {
            Show(null);
            if (selected != null)
                SelectElement(selected);
            if (measure)
                SetMeasureMode(true);
        }
        else
        {
            Hide();
            if (selected != null)
                RemoteSelect(selected);
            _measureMode = measure;
            _highlightLayer?.SetSelectMode(_remoteSelectMode || _measureMode);
        }
    }

    /// <summary>Clears the remote highlight-only overlay (no-op when the full panel is open).</summary>
    public void RemoteClearHighlight()
    {
        if (IsShown && _panelLayer == null)
            Hide();
    }

    /// <summary>Called after the web client edited a value or ran a structural action.</summary>
    public void RemoteAfterEdit()
    {
        OnPropertyEdited();
        if (_panelLayer != null && _selected is { } element)
        {
            var sections = InspectorServices.Properties.Collect(element, GetRectInWindow(element));
            _panelLayer.ShowSelection(element, sections, ParentChain(element), scrollTree: false, preservePropsScroll: true);
        }
    }

}
