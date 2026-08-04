using System.Net;
using Microsoft.Extensions.Logging;

namespace Immons.Tools.Maui.Inspector.Web.Hosting;

/// <summary>
/// Embedded HTTP server exposing the inspector to a desktop browser. Requests are routed
/// through the <see cref="IHttpEndpoint"/> chain; the server itself only owns the listener.
/// </summary>
internal sealed class RemoteServer
{
    /// <summary>Per-process nonce returned by /api/ping — lets the startup probe (and sibling
    /// panels) tell this instance apart from another process shadowing the port (adb forward…).</summary>
    public static readonly string InstanceId = Guid.NewGuid().ToString("N");

    static RemoteServer? _instance;
    static int _starting;

    readonly HttpListener _listener = new();
    readonly IReadOnlyList<IHttpEndpoint> _endpoints;
    readonly int _port;

    /// <summary>Base URL of the running server, null when not started.</summary>
    public static string? Url => _instance == null ? null : $"http://localhost:{_instance._port}";

    /// <summary>Why the server failed to start, null when fine (or not attempted).</summary>
    public static string? StartError { get; private set; }

    RemoteServer(int port, IReadOnlyList<IHttpEndpoint> endpoints)
    {
        _port = port;
        _endpoints = endpoints;
        _listener.Prefixes.Add($"http://*:{port}/");
        _listener.Start();
        _ = Task.Run(Loop);
    }

    const int AutoPortRangeStart = 9295;
    const int AutoPortRangeSize = 15;

    /// <summary>Null port = auto-assign from the standard range; a value forces that exact port.</summary>
    public static void EnsureStarted(int? port)
    {
        if (_instance != null || Interlocked.Exchange(ref _starting, 1) == 1)
            return;

        var (first, range) = port is { } forced ? (forced, 0) : (AutoPortRangeStart, AutoPortRangeSize - 1);
        // Off the main thread: probing can wait on sockets. Url stays null until ready.
        _ = Task.Run(() => StartWithFallback(first, range));
    }

    /// <summary>
    /// Several instances of the same app (multi-simulator hot reload) share one host, and a
    /// port can be shadowed by another process (adb forward binds 127.0.0.1 while our wildcard
    /// bind still "succeeds"). Each candidate is therefore verified by calling our own
    /// /api/ping through loopback and checking the instance nonce.
    /// </summary>
    static async Task StartWithFallback(int port, int range)
    {
        for (var candidate = port; candidate <= port + range; candidate++)
        {
            RemoteServer? server = null;
            try
            {
                server = new RemoteServer(candidate, EndpointFactory.CreateAll());
                if (await RespondsWithOurNonce(candidate).ConfigureAwait(false))
                {
                    _instance = server;
                    StartError = null;
                    Announce($"web inspector listening on http://localhost:{candidate}/ "
                        + (range == 0 ? "(forced port)" : "(auto-assigned)")
                        + AdbHint(candidate),
                        LogLevel.Information);
                    return;
                }

                StartError = $"port {candidate} is shadowed by another process";
                server.StopListening();
            }
            catch (Exception ex)
            {
                StartError = ex.Message;
                server?.StopListening();
            }
        }

        Announce($"failed to start the web inspector on port(s) {port}–{port + range}: {StartError}", LogLevel.Warning);
    }

    /// <summary>The adb-forward reminder only makes sense on Android.</summary>
    static string AdbHint(int port)
    {
        try
        {
            return DeviceInfo.Current.Platform == DevicePlatform.Android
                ? $" — Android emulator: run `adb forward tcp:{port} tcp:{port}` first"
                : "";
        }
        catch
        {
            return ""; // DeviceInfo unavailable (neutral TFM)
        }
    }

    /// <summary>Startup diagnostics go to the platform console and the panel's Logs view.</summary>
    static void Announce(string message, LogLevel level)
    {
        Console.WriteLine("[MauiInspector] " + message);
        InspectorServices.Logs.Write(level, "MauiInspector", message);
    }

    static async Task<bool> RespondsWithOurNonce(int port)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var body = await client.GetStringAsync($"http://127.0.0.1:{port}{ApiRoutes.Broadcast.Ping}").ConfigureAwait(false);
            return body.Contains(InstanceId, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    void StopListening()
    {
        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch
        {
            // already dead — nothing to release
        }
    }

    async Task Loop()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch
            {
                break;
            }

            _ = Task.Run(() => HandleSafe(context));
        }
    }

    async Task HandleSafe(HttpListenerContext context)
    {
        try
        {
            await Handle(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            try
            {
                await HttpResponse.WriteText(context, 500, ex.Message).ConfigureAwait(false);
            }
            catch
            {
                // client went away
            }
        }
    }

    async Task Handle(HttpListenerContext context)
    {
        var path = context.Request.Url?.AbsolutePath ?? "/";
        var method = context.Request.HttpMethod;

        foreach (var endpoint in _endpoints)
        {
            if (await endpoint.TryHandle(context, method, path).ConfigureAwait(false))
                return;
        }

        await HttpResponse.WriteText(context, 404, "not found").ConfigureAwait(false);
    }
}
