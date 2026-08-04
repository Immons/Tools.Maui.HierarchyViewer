using CoreAnimation;
using Foundation;

namespace Immons.Tools.Maui.Inspector.Features.Performance;

internal static partial class FrameStats
{
    static CADisplayLink? _displayLink;

    static partial void StartPlatform()
    {
        _displayLink = CADisplayLink.Create(() =>
        {
            if (_displayLink is { } link)
                OnFrame(link.Timestamp * 1000.0);
        });
        _displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
    }

    static partial void StopPlatform()
    {
        _displayLink?.Invalidate();
        _displayLink = null;
    }
}
