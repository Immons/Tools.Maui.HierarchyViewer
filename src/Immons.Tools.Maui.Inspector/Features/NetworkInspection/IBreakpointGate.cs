namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

/// <summary>Pauses HTTP calls (request and/or response phase) until the panel decides.</summary>
internal interface IBreakpointGate
{
    bool CaptureRequests { get; }

    bool CaptureResponses { get; }

    string UrlFilter { get; }

    IReadOnlyList<InterceptedCall> Pending { get; }

    void Configure(bool captureRequests, bool captureResponses, string urlFilter);

    long NextId();

    bool ShouldCapture(InterceptPhase phase, string url);

    /// <summary>
    /// Parks the call until the panel resumes/aborts it. Returns null when the wait was
    /// cancelled (HttpClient timeout) — the caller then proceeds with the original data.
    /// </summary>
    Task<InterceptDecision?> WaitAsync(InterceptedCall call, CancellationToken cancellationToken);

    bool Resume(long id, string? body, int? status);

    bool Abort(long id);
}
