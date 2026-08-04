namespace Immons.Tools.Maui.Inspector.Features.XamlSync;

/// <summary>XAML source locations of live elements, recorded by MAUI when diagnostics are on.</summary>
internal static class XamlSource
{
    /// <summary>
    /// Source info registration is gated behind a runtime feature switch; it must be flipped
    /// before any XAML page is inflated (called from UseMauiInspector).
    /// </summary>
    public static void EnableDiagnostics() =>
        AppContext.SetSwitch("Microsoft.Maui.RuntimeFeature.EnableMauiDiagnostics", true);

    public static string? Describe(object element)
    {
        try
        {
            return Microsoft.Maui.VisualDiagnostics.GetSourceInfo(element) is { } info
                ? $"{info.SourceUri}:{info.LineNumber}:{info.LinePosition}"
                : null;
        }
        catch
        {
            return null;
        }
    }
}
