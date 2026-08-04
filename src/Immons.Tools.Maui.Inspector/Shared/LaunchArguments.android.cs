namespace Immons.Tools.Maui.Inspector.Shared;

internal static partial class LaunchArguments
{
    // Extras belong to the launch intent, so nothing can be read until the activity exists —
    // MauiProgram runs earlier than that.
    internal static partial bool Ready => Platform.CurrentActivity != null;

    internal static partial string? PlatformValue(string name) =>
        Platform.CurrentActivity?.Intent?.GetStringExtra(name);
}
