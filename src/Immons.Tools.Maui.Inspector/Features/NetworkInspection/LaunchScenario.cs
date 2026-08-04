using Immons.Tools.Maui.Inspector.Shared;

namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

/// <summary>
/// Scenario selected by a launch argument, for UI tests that must decide before the app's first
/// call. Applied in memory only — a test run never overwrites what the developer picked in the panel.
/// </summary>
internal static class LaunchScenario
{
    public const string ArgumentName = "inspectorScenario";

    /// <summary>Suspends mocking entirely (same as the picker's "off").</summary>
    public const string Off = "off";

    /// <summary>Global rules only (same as the picker's "(none)").</summary>
    public const string None = "none";

    /// <summary>Requested value, or null when the argument was not passed (or is not readable yet).</summary>
    public static string? Requested()
    {
        var value = LaunchArguments.Value(ArgumentName)?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>False while the platform cannot answer yet, so the lookup is worth retrying.</summary>
    public static bool Resolvable => LaunchArguments.Ready;
}
