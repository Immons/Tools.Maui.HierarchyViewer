namespace Immons.Tools.Maui.Inspector.Inspector;

internal sealed partial class WindowInspector
{

    void SelectAt(Point windowPoint)
    {
        var hit = HitTester.HitTest(RootElements(), windowPoint, GetRectInWindow);
        if (hit == null)
            return;

        if (_measureMode && _selected != null)
        {
            SetCompareElement(hit);
        }
        else if (_panelLayer == null && IsShown)
        {
            // Remote highlight-only mode: a tap picks the primary element.
            _selected = hit;
            _compare = null;
            UpdateHighlight();
        }
        else
        {
            SelectElement(hit);
        }
    }

    public void SelectElement(VisualElement element, bool scrollTree = true)
    {
        if (!IsShown || _panelLayer == null)
            return;

        _selected = element;
        if (_compare != null && ReferenceEquals(_compare, element))
            _compare = null;

        UpdateHighlight();

        if (!_panelLayer.TreeContains(element))
            RefreshTree();

        var sections = InspectorServices.Properties.Collect(element, GetRectInWindow(element));
        _panelLayer.ShowSelection(element, sections, ParentChain(element), scrollTree);
    }

    void SetCompareElement(VisualElement element)
    {
        if (!IsShown || _selected == null)
            return;

        // Tapping the primary again clears the compare target.
        if (ReferenceEquals(element, _selected))
        {
            _compare = null;
            UpdateHighlight();
            return;
        }

        _compare = element;
        UpdateHighlight();
    }

    void SetSelectMode(bool on)
    {
        if (_measureMode && !on)
            on = true;
        _highlightLayer?.SetSelectMode(on);
        _panelLayer?.SetSelectModeVisual(on);
    }

    void SetMeasureMode(bool on)
    {
        _measureMode = on;
        if (on)
        {
            SetSelectMode(true);
            _compare = null;
            UpdateHighlight();
        }
        else
        {
            _compare = null;
            UpdateHighlight();
        }
        _panelLayer?.SetMeasureModeVisual(on);
    }
}
