using Android.Views;

namespace Immons.Tools.Maui.Inspector.Features.Performance;

internal static partial class FrameStats
{
    sealed class FrameCallback : Java.Lang.Object, Choreographer.IFrameCallback
    {
        public void DoFrame(long frameTimeNanos)
        {
            if (!Enabled)
                return;
            OnFrame(frameTimeNanos / 1_000_000.0);
            Choreographer.Instance?.PostFrameCallback(this);
        }
    }

    static FrameCallback? _callback;

    static partial void StartPlatform()
    {
        _callback ??= new FrameCallback();
        Choreographer.Instance?.PostFrameCallback(_callback);
    }

    static partial void StopPlatform()
    {
        if (_callback != null)
            Choreographer.Instance?.RemoveFrameCallback(_callback);
    }
}
