using System.Collections.Concurrent;
using Immons.Tools.Maui.Inspector.Features.NetworkInspection.Storage;
using Immons.Tools.Maui.Inspector.Shared.Storage;

namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

/// <summary>Pending calls keyed by id; disabling a phase releases its parked calls unmodified.</summary>
internal sealed class BreakpointGate : IBreakpointGate
{
    readonly ConcurrentDictionary<long, InterceptedCall> _pending = new();
    long _nextId;

    volatile bool _captureRequests;
    volatile bool _captureResponses;
    volatile string _urlFilter = "";

    static IBreakpointStore Store => InspectorStorage.Current.Breakpoints;

    public BreakpointGate()
    {
        // Restored on startup so the first requests already pause.
        Load();
        InspectorStorage.Changed += Load;
    }

    void Load()
    {
        var settings = Store.Load();
        _captureRequests = settings.CaptureRequests;
        _captureResponses = settings.CaptureResponses;
        _urlFilter = settings.UrlFilter;
    }

    public bool CaptureRequests => _captureRequests;

    public bool CaptureResponses => _captureResponses;

    public string UrlFilter => _urlFilter;

    public IReadOnlyList<InterceptedCall> Pending =>
        _pending.Values.OrderBy(c => c.Id).ToList();

    public void Configure(bool captureRequests, bool captureResponses, string urlFilter)
    {
        _captureRequests = captureRequests;
        _captureResponses = captureResponses;
        _urlFilter = urlFilter.Trim();

        Store.Save(new BreakpointSettings(captureRequests, captureResponses, _urlFilter));

        // Turning a phase off must not leave calls parked forever.
        foreach (var call in _pending.Values)
        {
            var stillOn = call.Phase == InterceptPhase.Request ? _captureRequests : _captureResponses;
            if (!stillOn)
                Resume(call.Id, body: null, status: null);
        }
    }

    public bool ShouldCapture(InterceptPhase phase, string url)
    {
        var on = phase == InterceptPhase.Request ? _captureRequests : _captureResponses;
        if (!on)
            return false;
        var filter = _urlFilter;
        return filter.Length == 0 || url.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    public long NextId() => Interlocked.Increment(ref _nextId);

    public async Task<InterceptDecision?> WaitAsync(InterceptedCall call, CancellationToken cancellationToken)
    {
        _pending[call.Id] = call;
        try
        {
            await using var registration = cancellationToken.Register(
                () => call.Decision.TrySetCanceled(cancellationToken));
            return await call.Decision.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // HttpClient.Timeout fired while the call sat in the panel — let it continue as-is.
            return null;
        }
        finally
        {
            _pending.TryRemove(call.Id, out _);
        }
    }

    public bool Resume(long id, string? body, int? status) =>
        _pending.TryGetValue(id, out var call)
        && call.Decision.TrySetResult(new InterceptDecision(false, body, status));

    public bool Abort(long id) =>
        _pending.TryGetValue(id, out var call)
        && call.Decision.TrySetResult(new InterceptDecision(true, null, null));
}
