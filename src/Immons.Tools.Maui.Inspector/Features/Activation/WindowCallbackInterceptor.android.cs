using Android.Views;
using Android.Views.Accessibility;
using AView = Android.Views.View;
using AWindow = Android.Views.Window;

namespace Immons.Tools.Maui.Inspector.Features.Activation;

/// <summary>
/// Wraps the activity's Window.Callback to observe every touch event (before any view consumes it)
/// without altering dispatch. Used for global long-press detection.
/// </summary>
internal sealed class WindowCallbackInterceptor : Java.Lang.Object, AWindow.ICallback
{
    readonly AWindow.ICallback _wrapped;
    readonly LongPressDetector _detector;

    public WindowCallbackInterceptor(AWindow.ICallback wrapped, LongPressDetector detector)
    {
        _wrapped = wrapped;
        _detector = detector;
    }

    public AWindow.ICallback Wrapped => _wrapped;
    public LongPressDetector Detector => _detector;

    public bool DispatchTouchEvent(MotionEvent? e)
    {
        if (e != null)
            _detector.OnTouchEvent(e);
        return _wrapped.DispatchTouchEvent(e);
    }

    public bool DispatchGenericMotionEvent(MotionEvent? e) => _wrapped.DispatchGenericMotionEvent(e);
    public bool DispatchKeyEvent(KeyEvent? e) => _wrapped.DispatchKeyEvent(e);
    public bool DispatchKeyShortcutEvent(KeyEvent? e) => _wrapped.DispatchKeyShortcutEvent(e);
    public bool DispatchPopulateAccessibilityEvent(AccessibilityEvent? e) => _wrapped.DispatchPopulateAccessibilityEvent(e);
    public bool DispatchTrackballEvent(MotionEvent? e) => _wrapped.DispatchTrackballEvent(e);
    public void OnActionModeFinished(ActionMode? mode) => _wrapped.OnActionModeFinished(mode);
    public void OnActionModeStarted(ActionMode? mode) => _wrapped.OnActionModeStarted(mode);
    public void OnAttachedToWindow() => _wrapped.OnAttachedToWindow();
    public void OnContentChanged() => _wrapped.OnContentChanged();
    public bool OnCreatePanelMenu(int featureId, IMenu menu) => _wrapped.OnCreatePanelMenu(featureId, menu);
    public AView? OnCreatePanelView(int featureId) => _wrapped.OnCreatePanelView(featureId);
    public void OnDetachedFromWindow() => _wrapped.OnDetachedFromWindow();
    public bool OnMenuItemSelected(int featureId, IMenuItem item) => _wrapped.OnMenuItemSelected(featureId, item);
    public bool OnMenuOpened(int featureId, IMenu menu) => _wrapped.OnMenuOpened(featureId, menu);
    public void OnPanelClosed(int featureId, IMenu menu) => _wrapped.OnPanelClosed(featureId, menu);
    public bool OnPreparePanel(int featureId, AView? view, IMenu menu) => _wrapped.OnPreparePanel(featureId, view, menu);
    public bool OnSearchRequested() => _wrapped.OnSearchRequested();
    public bool OnSearchRequested(SearchEvent? searchEvent) => _wrapped.OnSearchRequested(searchEvent);
    public void OnWindowAttributesChanged(WindowManagerLayoutParams? attrs) => _wrapped.OnWindowAttributesChanged(attrs);
    public void OnWindowFocusChanged(bool hasFocus) => _wrapped.OnWindowFocusChanged(hasFocus);
    public ActionMode? OnWindowStartingActionMode(ActionMode.ICallback? callback) => _wrapped.OnWindowStartingActionMode(callback);
    public ActionMode? OnWindowStartingActionMode(ActionMode.ICallback? callback, ActionModeType type) => _wrapped.OnWindowStartingActionMode(callback, type);
}
