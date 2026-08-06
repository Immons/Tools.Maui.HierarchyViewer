using System.Net;

namespace Immons.Tools.Maui.Inspector.Features.Mirroring;

/// <summary>GET /api/screenshot (PNG) and POST /api/select-at (mirror click hit-test).</summary>
internal sealed class MirrorEndpoint(
    IMainThreadDispatcher mainThread,
    IActiveInspectorProvider inspectors) : IHttpEndpoint
{
    public async Task<bool> TryHandle(HttpListenerContext context, string method, string path)
    {
        if (method == HttpVerbs.Get && path == ApiRoutes.Mirror.Screenshot)
        {
            var bytes = await mainThread.RunTaskAsync(CaptureComposited).ConfigureAwait(false);
            await HttpResponse.WriteBytes(context, "image/png", bytes).ConfigureAwait(false);
            return true;
        }

        if (method == HttpVerbs.Post && path == ApiRoutes.Mirror.SelectAt)
        {
            var node = await RequestBody.ReadJson(context).ConfigureAwait(false);
            var x = node?["x"]?.GetValue<double>() ?? 0;
            var y = node?["y"]?.GetValue<double>() ?? 0;
            var ok = await mainThread.RunAsync(() =>
                inspectors.Current?.RemoteSelectAt(new Point(x, y)) ?? false).ConfigureAwait(false);
            await HttpResponse.WriteOk(context, ok).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    /// <summary>Inspector-composited capture first — Essentials' Screenshot misses the
    /// separate windows Android hosts modal pages in.</summary>
    async Task<byte[]> CaptureComposited()
    {
        if (inspectors.Current?.CapturePng() is { } composited)
            return composited;
        return await Capture();
    }

    static async Task<byte[]> Capture()
    {
        var shot = await Screenshot.Default.CaptureAsync();
        using var stream = await shot.OpenReadAsync(ScreenshotFormat.Png);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }
}
