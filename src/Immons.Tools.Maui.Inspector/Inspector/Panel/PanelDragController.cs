namespace Immons.Tools.Maui.Inspector.Inspector.Panel;

/// <summary>
/// Panel dragging: pan gestures on registered surfaces move the panel, clamped to the window.
/// The offset is applied on the host platform view (frame/transform) because the panel is
/// parented outside the normal MAUI layout tree (UIWindow / DecorView), where MAUI
/// TranslationX/Y alone often has no effect.
/// </summary>
internal sealed class PanelDragController(View panel)
{
    double _dragX;
    double _dragY;

    /// <summary>Window size in dp, used to clamp panel dragging. Set by the inspector.</summary>
    public Func<Size>? WindowSizeProvider { get; set; }

    /// <summary>Applies the drag offset on the host platform view.</summary>
    public Action<double, double>? ApplyDragOffset { get; set; }

    public double OffsetX => _dragX;

    public double OffsetY => _dragY;

    /// <summary>Makes the target a drag surface for the panel.</summary>
    public void Attach(View target)
    {
        var pan = new PanGestureRecognizer();
        double startX = 0, startY = 0;
        pan.PanUpdated += (_, e) =>
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    startX = _dragX;
                    startY = _dragY;
                    break;
                case GestureStatus.Running:
                    SetOffset(startX + e.TotalX, startY + e.TotalY);
                    break;
                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    // Keep final offset; nothing else to do.
                    break;
            }
        };
        target.GestureRecognizers.Add(pan);
    }

    public void Reset() => SetOffset(0, 0, clamp: false);

    void SetOffset(double x, double y, bool clamp = true)
    {
        if (clamp
            && WindowSizeProvider?.Invoke() is { Width: > 0, Height: > 0 } window
            && !double.IsNaN(window.Width)
            && panel.Height > 0 && panel.Width > 0)
        {
            var minY = -(window.Height - panel.Height);
            var maxY = panel.Height - 56;
            y = Math.Clamp(y, minY, maxY);

            var maxX = window.Width - 72;
            x = Math.Clamp(x, -maxX, maxX);
        }

        _dragX = x;
        _dragY = y;

        // Platform host (UIWindow / DecorView) — this is what actually moves the panel.
        ApplyDragOffset?.Invoke(x, y);

        // Fallback for hosts that do honor MAUI translation.
        panel.TranslationX = x;
        panel.TranslationY = y;
    }
}
