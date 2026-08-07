using System.Net;
using System.Net.Sockets;
using System.Text.Json.Nodes;

namespace Immons.Tools.Maui.Inspector.Sync;

/// <summary>
/// Host-side adb plumbing. An Android emulator has its own loopback, so an inspector
/// listening on the device's port 9295 is invisible from the host — and a 1:1
/// `adb forward` can collide with an iOS simulator app, which shares the host network
/// and may already occupy that host port. This finds inspector instances on every
/// connected Android device and maps each onto a free host port from the standard
/// scan range, so the browser and the Devices tab see them all side by side.
/// </summary>
internal static class AdbForwarder
{
    public sealed record Forward(string Serial, int HostPort, int DevicePort, string Device);

    public static async Task<List<Forward>> EnsureForwards(HttpClient http, int[] candidatePorts)
    {
        var result = new List<Forward>();
        List<string> serials;
        try
        {
            serials = await Devices();
        }
        catch
        {
            return result; // adb not installed — iOS-only setup
        }
        if (serials.Count == 0)
            return result;

        var existing = await ExistingForwards();

        foreach (var serial in serials)
        {
            foreach (var devicePort in candidatePorts)
            {
                // A live mapping from a previous run is reused, a dead one replaced.
                var known = existing.FirstOrDefault(f => f.Serial == serial && f.DevicePort == devicePort);
                if (known != null)
                {
                    if (await ProbeInspector(http, known.HostPort) is { } aliveDevice)
                    {
                        result.Add(known with { Device = aliveDevice });
                        continue;
                    }
                    await Adb($"-s {serial} forward --remove tcp:{known.HostPort}");
                }

                // Peek through a temporary forward to see whether an inspector listens there.
                var probe = await Adb($"-s {serial} forward tcp:0 tcp:{devicePort}");
                if (probe.Code != 0 || !int.TryParse(probe.Output.Trim(), out var tempPort))
                    continue;
                var device = await ProbeInspector(http, tempPort);
                await Adb($"-s {serial} forward --remove tcp:{tempPort}");
                if (device == null)
                    continue;

                if (PickFreeHostPort(candidatePorts, devicePort) is not { } hostPort)
                {
                    Console.WriteLine($"{serial}: inspector on device port {devicePort}, but no free host port in {candidatePorts[0]}-{candidatePorts[^1]}");
                    continue;
                }
                if ((await Adb($"-s {serial} forward tcp:{hostPort} tcp:{devicePort}")).Code == 0)
                    result.Add(new Forward(serial, hostPort, devicePort, device));
            }
        }
        return result;
    }

    /// <summary>Serial set for change detection; empty when adb is unavailable.</summary>
    public static async Task<HashSet<string>> SerialsSafe()
    {
        try
        {
            return [.. await Devices()];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Serials of ready devices/emulators; throws when adb is missing.</summary>
    static async Task<List<string>> Devices()
    {
        var (code, output) = await Adb("devices");
        if (code != 0)
            return [];
        return output.Split('\n')
            .Skip(1)
            .Select(line => line.Trim().Split('\t'))
            .Where(parts => parts.Length == 2 && parts[1] == "device")
            .Select(parts => parts[0])
            .ToList();
    }

    static async Task<List<Forward>> ExistingForwards()
    {
        var result = new List<Forward>();
        var (code, output) = await Adb("forward --list");
        if (code != 0)
            return result;
        foreach (var line in output.Split('\n'))
        {
            // "emulator-5554 tcp:9500 tcp:9308"
            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3
                && parts[1].StartsWith("tcp:", StringComparison.Ordinal)
                && parts[2].StartsWith("tcp:", StringComparison.Ordinal)
                && int.TryParse(parts[1][4..], out var host)
                && int.TryParse(parts[2][4..], out var device))
                result.Add(new Forward(parts[0], host, device, ""));
        }
        return result;
    }

    /// <summary>Device description when an inspector answers on this host port; null otherwise.</summary>
    static async Task<string?> ProbeInspector(HttpClient http, int port)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(700));
            var json = JsonNode.Parse(await http.GetStringAsync($"http://localhost:{port}/api/tree", cts.Token));
            return json?["device"]?.GetValue<string>() ?? "";
        }
        catch
        {
            return null;
        }
    }

    /// <summary>The device's own port when free on the host, else the first free one in range.</summary>
    static int? PickFreeHostPort(int[] candidatePorts, int preferred)
    {
        foreach (var port in candidatePorts.OrderBy(p => p == preferred ? 0 : 1))
        {
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return port;
            }
            catch (SocketException)
            {
                // occupied — an iOS app, another forward, anything
            }
        }
        return null;
    }

    static async Task<(int Code, string Output)> Adb(string arguments)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("adb", arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("adb not found");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, output.Length > 0 ? output : error);
    }
}
