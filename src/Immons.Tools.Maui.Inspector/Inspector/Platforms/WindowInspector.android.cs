using Android.App;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace Immons.Tools.Maui.Inspector.Inspector;

internal sealed partial class WindowInspector
{
    Activity? _activity;
    WindowCallbackInterceptor? _interceptor;
    AView? _highlightPlatform;
    AView? _panelPlatform;

    private partial void AttachPlatform()
    {
        _activity = _window.Handler?.PlatformView as Activity;
        var platformWindow = _activity?.Window;
        if (_activity == null || platformWindow?.Callback == null || platformWindow.Callback is WindowCallbackInterceptor)
            return;

        var detector = new LongPressDetector(_activity, _options, OnLongPressPx);
        _interceptor = new WindowCallbackInterceptor(platformWindow.Callback, detector);
        platformWindow.Callback = _interceptor;
    }

    private partial void DetachPlatform()
    {
        var platformWindow = _activity?.Window;
        if (platformWindow != null && _interceptor != null && ReferenceEquals(platformWindow.Callback, _interceptor))
            platformWindow.Callback = _interceptor.Wrapped;
        _interceptor = null;
        _activity = null;
    }

    void OnLongPressPx(float xPx, float yPx)
    {
        var d = Density;
        OnLongPressDetected(new Point(xPx / d, yPx / d));
    }

    double Density => _activity?.Resources?.DisplayMetrics?.Density ?? 1;

    /// <summary>
    /// The view group the overlay layers live in. MAUI hosts each modal page in its own
    /// platform window (a Dialog) stacked above the activity, so layers pinned to the
    /// activity's decor would render — and hit-test — beneath an open modal. The host is
    /// therefore the decor of the topmost modal's window, falling back to the activity.
    /// </summary>
    ViewGroup? HostDecor()
    {
        var decor = _activity?.Window?.DecorView as ViewGroup;

        IReadOnlyList<Page>? modals = null;
        try { modals = _window.Page?.Navigation?.ModalStack; }
        catch { /* navigation may be unavailable mid-teardown */ }

        for (var i = (modals?.Count ?? 0) - 1; i >= 0; i--)
        {
            if (modals![i].Handler?.PlatformView is AView view
                && view.IsAttachedToWindow
                && view.RootView is ViewGroup modalRoot
                && !ReferenceEquals(modalRoot, decor))
                return modalRoot;
        }

        return decor;
    }

    private partial void AddLayersPlatform()
    {
        if (HostDecor() is not { } decor || _mauiContext == null)
            return;

        // Keep long-press detection alive in remote highlight-only mode (no panel).
        if (_interceptor != null)
            _interceptor.Detector.Suspended = _panelLayer != null;

        _highlightPlatform = _highlightLayer!.ToPlatform(_mauiContext);
        decor.AddView(_highlightPlatform, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

        if (_panelLayer == null)
            return; // remote highlight-only mode

        _panelPlatform = _panelLayer.ToPlatform(_mauiContext);
        var height = (int)(decor.Height * _options.PanelHeightFraction);
        if (height <= 0)
            height = (int)(400 * Density);
        decor.AddView(_panelPlatform, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, height) { Gravity = GravityFlags.Bottom });
        _panelPlatform.Clickable = true;
        _panelPlatform.Focusable = true;
        SetPanelOffsetPlatform(_panelLayer.DragOffsetX, _panelLayer.DragOffsetY);
    }

    private partial void RemoveLayersPlatform()
    {
        if (_interceptor != null)
            _interceptor.Detector.Suspended = false;

        if (_highlightPlatform?.Parent is ViewGroup p1)
            p1.RemoveView(_highlightPlatform);
        if (_panelPlatform?.Parent is ViewGroup p2)
            p2.RemoveView(_panelPlatform);
        _highlightPlatform = null;
        _panelPlatform = null;
    }

    private partial void SetPanelOffsetPlatform(double xDp, double yDp)
    {
        if (_panelPlatform == null)
            return;
        var d = (float)Density;
        _panelPlatform.TranslationX = (float)xDp * d;
        _panelPlatform.TranslationY = (float)yDp * d;
    }

    // Screen (not window) coordinates throughout: elements and overlay layers can live in
    // different platform windows (activity vs modal dialog), and screen space is the only
    // frame they share.
    private partial Rect? GetRectInWindowPlatform(VisualElement element)
    {
        if (element.Handler?.PlatformView is not AView pv || !pv.IsAttachedToWindow)
            return null;

        var loc = new int[2];
        pv.GetLocationOnScreen(loc);
        var d = Density;
        return new Rect(loc[0] / d, loc[1] / d, pv.Width / d, pv.Height / d);
    }

    private partial Point GetLayerOriginPlatform()
    {
        if (_highlightPlatform == null || !_highlightPlatform.IsAttachedToWindow)
            return Point.Zero;

        var loc = new int[2];
        _highlightPlatform.GetLocationOnScreen(loc);
        var d = Density;
        return new Point(loc[0] / d, loc[1] / d);
    }

    /// <summary>
    /// Composites the activity window and every modal window into one PNG. Essentials'
    /// Screenshot captures only the activity window, so with a modal open the mirror
    /// would show the page underneath it. Null when no modal is up — the regular
    /// screenshot path is both correct and cheaper then.
    /// </summary>
    private partial byte[]? CapturePngPlatform()
    {
        if (_activity?.Window?.DecorView is not ViewGroup decor || decor.Width <= 0 || decor.Height <= 0)
            return null;

        IReadOnlyList<Page>? modals = null;
        try { modals = _window.Page?.Navigation?.ModalStack; }
        catch { /* navigation may be unavailable mid-teardown */ }
        if (modals == null || modals.Count == 0)
            return null;

        using var bitmap = Android.Graphics.Bitmap.CreateBitmap(
            decor.Width, decor.Height, Android.Graphics.Bitmap.Config.Argb8888!);
        var canvas = new Android.Graphics.Canvas(bitmap);
        var origin = new int[2];
        decor.GetLocationOnScreen(origin);
        decor.Draw(canvas);

        var drawn = new HashSet<AView> { decor };
        foreach (var modal in modals)
        {
            if (modal.Handler?.PlatformView is not AView view || !view.IsAttachedToWindow)
                continue;
            var root = view.RootView;
            if (root == null || !drawn.Add(root))
                continue;

            var loc = new int[2];
            root.GetLocationOnScreen(loc);
            canvas.Save();
            canvas.Translate(loc[0] - origin[0], loc[1] - origin[1]);
            root.Draw(canvas);
            canvas.Restore();
        }

        using var stream = new MemoryStream();
        bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png!, 100, stream);
        return stream.ToArray();
    }

    private partial double GetBottomInsetPlatform()
    {
        var decor = _activity?.Window?.DecorView;
        if (decor == null)
            return 0;

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var insets = decor.RootWindowInsets?.GetInsets(WindowInsets.Type.SystemBars());
            return (insets?.Bottom ?? 0) / Density;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
#pragma warning disable CA1422 // deprecated on API 30+, guarded above
            return (decor.RootWindowInsets?.SystemWindowInsetBottom ?? 0) / Density;
#pragma warning restore CA1422
        }

        return 0;
    }
}
