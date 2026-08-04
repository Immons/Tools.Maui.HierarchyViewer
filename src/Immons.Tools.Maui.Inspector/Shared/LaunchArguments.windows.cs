namespace Immons.Tools.Maui.Inspector.Shared;

internal static partial class LaunchArguments
{
    internal static partial bool Ready => true;

    // Nothing platform-specific: WinUI apps get plain command-line arguments.
    internal static partial string? PlatformValue(string name) => null;
}
