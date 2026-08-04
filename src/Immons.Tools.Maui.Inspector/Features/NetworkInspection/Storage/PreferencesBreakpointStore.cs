namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection.Storage;

/// <summary>Breakpoint settings as one "req|resp|filter" Preferences entry.</summary>
internal sealed class PreferencesBreakpointStore : IBreakpointStore
{
    const string PreferencesKey = "hv_breakpoints";

    public BreakpointSettings Load()
    {
        try
        {
            var stored = Preferences.Default.Get<string?>(PreferencesKey, null);
            return stored?.Split('|', 3) is [var req, var resp, var filter]
                ? new BreakpointSettings(req == "1", resp == "1", filter)
                : BreakpointSettings.Off;
        }
        catch
        {
            // Preferences unavailable — start with breakpoints off.
            return BreakpointSettings.Off;
        }
    }

    public void Save(BreakpointSettings settings)
    {
        try
        {
            Preferences.Default.Set(PreferencesKey,
                $"{(settings.CaptureRequests ? 1 : 0)}|{(settings.CaptureResponses ? 1 : 0)}|{settings.UrlFilter}");
        }
        catch
        {
            // see Load
        }
    }
}
