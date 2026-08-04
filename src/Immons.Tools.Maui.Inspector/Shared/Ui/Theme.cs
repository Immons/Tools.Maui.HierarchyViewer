namespace Immons.Tools.Maui.Inspector.Shared.Ui;

/// <summary>Fixed dark palette for the inspector UI (independent of the host app's theme).</summary>
internal static class Theme
{
    public static readonly Color PanelBg = Color.FromArgb("#F51E2028");
    public static readonly Color PanelBg2 = Color.FromArgb("#FF2A2D39");
    public static readonly Color Accent = Color.FromArgb("#FF5C9EFF");
    public static readonly Color TextPrimary = Color.FromArgb("#FFECECF1");
    public static readonly Color TextSecondary = Color.FromArgb("#FF9BA0AE");
    public static readonly Color RowSelected = Color.FromArgb("#FF33415C");
    public static readonly Color Divider = Color.FromArgb("#FF3A3D4A");

    // Box model fills (Chrome DevTools-like)
    public static readonly Color MarginFill = Color.FromArgb("#59F6B26B");
    public static readonly Color PaddingFill = Color.FromArgb("#5993C47D");
    public static readonly Color ContentFill = Color.FromArgb("#476FA8DC");
    public static readonly Color Outline = Color.FromArgb("#FFFF4081");
    public static readonly Color Guide = Color.FromArgb("#66FF4081");
    public static readonly Color DimLabelBg = Color.FromArgb("#E6202124");

    // Measure / compare mode (second element + gaps)
    public static readonly Color CompareOutline = Color.FromArgb("#FF7C4DFF");
    public static readonly Color CompareFill = Color.FromArgb("#337C4DFF");
    public static readonly Color DistanceLine = Color.FromArgb("#FFFF4081");
    public static readonly Color DistanceLabelBg = Color.FromArgb("#E6FF4081");
    public static readonly Color MeasureAccent = Color.FromArgb("#FF7C4DFF");

    public const double FontSize = 12;
    public const double FontSizeSmall = 11;

    public static string MonoFont =>
        DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst ? "Menlo"
        : DeviceInfo.Platform == DevicePlatform.WinUI ? "Consolas"
        : "monospace";

    public static Label MakeLabel(string text = "", Color? color = null, double size = FontSize, bool bold = false) => new()
    {
        Text = text,
        TextColor = color ?? TextPrimary,
        FontSize = size,
        FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        VerticalOptions = LayoutOptions.Center,
        LineBreakMode = LineBreakMode.TailTruncation,
    };

    public static Button MakeButton(string text) => new()
    {
        Text = text,
        FontSize = 13,
        TextColor = TextPrimary,
        BackgroundColor = PanelBg2,
        Padding = new Thickness(10, 4),
        Margin = new Thickness(0),
        MinimumHeightRequest = 30,
        MinimumWidthRequest = 36,
        HeightRequest = 30,
        CornerRadius = 6,
        BorderWidth = 0,
        VerticalOptions = LayoutOptions.Center,
    };
}
