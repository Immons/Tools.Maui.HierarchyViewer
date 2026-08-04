using Foundation;

namespace Immons.Tools.Maui.Inspector.Shared;

internal static partial class LaunchArguments
{
    // iOS turns "-name value" launch arguments into user defaults, which is what Maestro and
    // Appium's processArguments produce.
    internal static partial bool Ready => true;

    internal static partial string? PlatformValue(string name) =>
        NSUserDefaults.StandardUserDefaults.StringForKey(name);
}
