namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection.Storage;

/// <summary>Breakpoint configuration that survives restarts, so the first calls already pause.</summary>
internal interface IBreakpointStore
{
    BreakpointSettings Load();

    void Save(BreakpointSettings settings);
}

/// <summary>Which phases pause and on which URLs ("" = every call).</summary>
internal readonly record struct BreakpointSettings(bool CaptureRequests, bool CaptureResponses, string UrlFilter)
{
    public static BreakpointSettings Off => new(false, false, "");
}
