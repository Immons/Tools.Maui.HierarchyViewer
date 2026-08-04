namespace Immons.Tools.Maui.Inspector.Shared.Ui;

internal static class LayoutExtensions
{
    /// <summary>
    /// The overlay manages window insets itself, so every layout inside it must not
    /// auto-adjust for the safe area (otherwise highlight geometry is offset on iOS).
    /// </summary>
    public static T NoSafeArea<T>(this T layout) where T : Layout
    {
#pragma warning disable CS0618 // SafeAreaElement replacement is still internal in MAUI 10
        layout.IgnoreSafeArea = true;
#pragma warning restore CS0618
        return layout;
    }
}
