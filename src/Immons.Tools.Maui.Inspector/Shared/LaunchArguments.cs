namespace Immons.Tools.Maui.Inspector.Shared;

/// <summary>
/// Values passed to the app at launch, so a UI test can pick what the inspector does before the
/// app makes its first call. Maestro's <c>launchApp: arguments:</c> and Appium's
/// <c>processArguments</c> / <c>optionalIntentArguments</c> both land here.
/// </summary>
internal static partial class LaunchArguments
{
    /// <summary>
    /// True once the platform can answer at all — on Android the extras live on the activity,
    /// which does not exist yet while MauiProgram runs, so lookups are retried until it does.
    /// </summary>
    internal static partial bool Ready { get; }

    /// <summary>Platform lookup; null when the argument was not passed.</summary>
    internal static partial string? PlatformValue(string name);

    public static string? Value(string name)
    {
        try
        {
            return PlatformValue(name) ?? FromCommandLine(name);
        }
        catch
        {
            // Reading launch arguments must never be a reason to fail.
            return null;
        }
    }

    /// <summary>Supports <c>-name value</c>, <c>--name value</c> and <c>--name=value</c>.</summary>
    internal static string? FromCommandLine(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Length == 0 || arg[0] != '-')
                continue;

            var key = arg.TrimStart('-');
            var separator = key.IndexOf('=');
            if (separator > 0)
            {
                if (key[..separator].Equals(name, StringComparison.OrdinalIgnoreCase))
                    return key[(separator + 1)..];
                continue;
            }

            if (key.Equals(name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1];
        }
        return null;
    }
}
