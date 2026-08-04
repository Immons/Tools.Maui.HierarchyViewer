namespace Immons.Tools.Maui.Inspector.Features.Performance;

/// <summary>
/// Frame timing sampler (Choreographer / CADisplayLink / CompositionTarget.Rendering).
/// Aggregates fps, average and worst frame time over ~half-second windows.
/// </summary>
internal static partial class FrameStats
{
    static readonly object Gate = new();
    static readonly List<double> FrameTimesMs = [];
    static double _lastTimestampMs;
    static double _fps, _averageMs, _worstMs;

    public static bool Enabled { get; private set; }

    public static (double Fps, double AverageMs, double WorstMs)? Current
    {
        get
        {
            if (!Enabled)
                return null;
            lock (Gate)
            {
                return (_fps, _averageMs, _worstMs);
            }
        }
    }

    /// <summary>Must be called on the UI thread.</summary>
    public static void SetEnabled(bool on)
    {
        if (on == Enabled)
            return;
        Enabled = on;

        lock (Gate)
        {
            FrameTimesMs.Clear();
            _lastTimestampMs = 0;
            _fps = _averageMs = _worstMs = 0;
        }

        if (on)
            StartPlatform();
        else
            StopPlatform();
    }

    /// <summary>Platform frame callback with a monotonic timestamp in milliseconds.</summary>
    static void OnFrame(double timestampMs)
    {
        if (_lastTimestampMs > 0)
        {
            var delta = timestampMs - _lastTimestampMs;
            if (delta is > 0 and < 2000)
            {
                lock (Gate)
                {
                    FrameTimesMs.Add(delta);
                    var total = FrameTimesMs.Sum();
                    if (total >= 500)
                    {
                        _fps = 1000.0 * FrameTimesMs.Count / total;
                        _averageMs = total / FrameTimesMs.Count;
                        _worstMs = FrameTimesMs.Max();
                        FrameTimesMs.Clear();
                    }
                }
            }
        }
        _lastTimestampMs = timestampMs;
    }

    static partial void StartPlatform();
    static partial void StopPlatform();
}
