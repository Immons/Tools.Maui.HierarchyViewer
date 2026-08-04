namespace Immons.Tools.Maui.Inspector.Features.XamlSync;

/// <summary>Connected = /api/changes was polled within the last few seconds.</summary>
internal sealed class SyncTracker : ISyncTracker
{
    const long NeverPolled = long.MinValue;
    const long FreshnessMs = 3500;

    long _lastPoll = NeverPolled;

    public void MarkPolled() => Volatile.Write(ref _lastPoll, Environment.TickCount64);

    public bool Connected
    {
        get
        {
            var last = Volatile.Read(ref _lastPoll);
            // The sentinel means "never polled" — the subtraction would overflow to negative.
            return last != NeverPolled && Environment.TickCount64 - last < FreshnessMs;
        }
    }
}
