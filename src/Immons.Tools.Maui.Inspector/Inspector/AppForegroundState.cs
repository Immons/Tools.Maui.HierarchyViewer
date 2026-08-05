namespace Immons.Tools.Maui.Inspector.Inspector;

/// <summary>
/// Whether the app is in the foreground. The embedded server keeps answering while the app is
/// backgrounded, so the panel would look connected while every edit silently waits for a main
/// thread that is no longer pumping — this is what tells the panel to say so.
/// </summary>
internal static class AppForegroundState
{
    // Deliberately Stopped/Resumed, not Deactivated/Activated: on desktop the app merely losing
    // focus still updates its UI just fine.
    static volatile bool _foreground = true;

    public static bool IsForeground => _foreground;

    public static void Track(Window window)
    {
        window.Stopped += (_, _) => _foreground = false;
        window.Resumed += (_, _) => _foreground = true;
        window.Activated += (_, _) => _foreground = true;
    }
}
