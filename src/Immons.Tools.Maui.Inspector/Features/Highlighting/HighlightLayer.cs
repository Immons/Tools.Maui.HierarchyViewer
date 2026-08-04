namespace Immons.Tools.Maui.Inspector.Features.Highlighting;

/// <summary>
/// Full-window layer that draws the box-model highlight and, while select mode is active,
/// captures taps to pick elements. Otherwise it is fully input-transparent.
/// </summary>
internal sealed class HighlightLayer : Grid
{
    readonly GraphicsView _canvas;
    readonly BoxModelDrawable _drawable = new();

    /// <summary>Tap position in this layer's coordinates (dp).</summary>
    public event Action<Point>? Tapped;

    public HighlightLayer()
    {
        // Nearly invisible but non-null background so the layer is hit-testable in select mode.
        BackgroundColor = Color.FromArgb("#01000000");
        CascadeInputTransparent = true;
        InputTransparent = true;
        this.NoSafeArea();

        _canvas = new GraphicsView
        {
            Drawable = _drawable,
            InputTransparent = true,
            BackgroundColor = Colors.Transparent,
        };
        Add(_canvas);

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, e) =>
        {
            if (e.GetPosition(this) is { } p)
                Tapped?.Invoke(p);
        };
        GestureRecognizers.Add(tap);
    }

    public void SetSelectMode(bool on) => InputTransparent = !on;

    public void ShowBox(BoxModel? model)
    {
        _drawable.Model = model;
        _canvas.Invalidate();
    }
}
