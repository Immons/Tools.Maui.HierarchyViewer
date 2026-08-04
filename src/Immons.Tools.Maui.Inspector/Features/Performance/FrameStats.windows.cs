using System.Diagnostics;

namespace Immons.Tools.Maui.Inspector.Features.Performance;

internal static partial class FrameStats
{
    static EventHandler<object>? _renderingHandler;

    static partial void StartPlatform()
    {
        _renderingHandler = (_, _) => OnFrame(Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency);
        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += _renderingHandler;
    }

    static partial void StopPlatform()
    {
        if (_renderingHandler != null)
            Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= _renderingHandler;
        _renderingHandler = null;
    }
}
