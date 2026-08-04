namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

/// <summary>
/// Records the live request path (method, URL, status, response body) and turns it into a
/// scenario: one no-network mock rule per unique call, tagged with the scenario name.
/// </summary>
internal interface IScenarioRecorder
{
    bool Recording { get; }

    /// <summary>Unique calls captured so far.</summary>
    int Count { get; }

    void Start();

    /// <summary>Called by the interceptor for every completed call; ignored when not recording.</summary>
    void Capture(string method, string url, int status, string? responseBody);

    /// <summary>Stops and saves the capture as scenario rules; returns how many rules were created.</summary>
    int StopAndSave(string scenarioName);

    void Cancel();
}
