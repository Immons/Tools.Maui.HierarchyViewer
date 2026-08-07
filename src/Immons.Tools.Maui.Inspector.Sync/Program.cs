using System.Text.Json.Nodes;
using Immons.Tools.Maui.Inspector.Sync;

var apps = new List<string>();
var src = Directory.GetCurrentDirectory();
var intervalMs = 1000;
var fromNow = false;
var dryRun = false;
var forwardOnly = false;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "forward":
            forwardOnly = true;
            break;
        case "--app" when i + 1 < args.Length:
            apps.AddRange(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(u => u.TrimEnd('/')));
            break;
        case "--src" when i + 1 < args.Length:
            src = Path.GetFullPath(args[++i]);
            break;
        case "--interval" when i + 1 < args.Length:
            intervalMs = int.Parse(args[++i]);
            break;
        case "--from-now":
            fromNow = true;
            break;
        case "--dry-run":
            dryRun = true;
            break;
        case "-h" or "--help":
            Console.WriteLine("""
                XAML Updater (maui-inspector-sync) — writes live MauiInspector edits back into your XAML sources.

                Typical use: cd into your app's source folder and just run

                    maui-inspector-sync

                It scans localhost ports 9295-9309 for a running inspector (sets up
                `adb forward` automatically when adb is available) and watches the current
                folder. Options for anything non-default:

                Just setting up Android port forwarding (no watching):

                    maui-inspector-sync forward

                It finds inspectors inside connected Android emulators/devices and maps each
                onto a free host port from the 9295-9309 range — useful when the "natural"
                port is already taken by e.g. an iOS simulator app.

                  --app        Base URL(s) of running web inspectors, comma-separated or repeated
                               (skips scanning). One updater serves every device at once.
                  --src        Root folder that contains the XAML sources (searched recursively).
                  --interval   Poll interval in milliseconds (default 1000).
                  --from-now   Ignore edits made before the updater started.
                  --dry-run    Print what would change without writing files.

                Enable recording with the "✎ XAML" button in the web inspector. Pair with your
                IDE's XAML Hot Reload for the full WYSIWYG loop.
                """);
            return 0;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]} (use --help)");
            return 1;
    }
}

if (!Directory.Exists(src))
{
    Console.Error.WriteLine($"Source folder not found: {src}");
    return 1;
}

var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

if (forwardOnly)
{
    var forwards = await AdbForwarder.EnsureForwards(http, Enumerable.Range(9295, 15).ToArray());
    if (forwards.Count == 0)
    {
        Console.WriteLine("no inspectors found on connected Android devices (is adb on PATH and the app running with the inspector enabled?)");
        return 1;
    }
    foreach (var forward in forwards)
        Console.WriteLine($"{forward.Serial}  device port {forward.DevicePort} → http://localhost:{forward.HostPort}  ({forward.Device})");
    Console.WriteLine("open any of the URLs above — the Devices tab of each sees the others.");
    return 0;
}

// No explicit --app: watch every inspector found, and keep adopting new ones as they appear.
// One app can be reachable on several host ports (old + new adb forwards) — the /api/ping
// instance nonce keeps each running app watched exactly once.
var watchedInstances = new HashSet<string>();
var autoDiscover = apps.Count == 0;
int[] scanPorts = Enumerable.Range(9295, 15).ToArray();
if (autoDiscover)
{
    Console.WriteLine("scanning localhost for running inspectors…");

    var forwards = await AdbForwarder.EnsureForwards(http, scanPorts);
    foreach (var forward in forwards)
        Console.WriteLine($"adb forward: {forward.Serial} device port {forward.DevicePort} → http://localhost:{forward.HostPort}  ({forward.Device})");

    apps.AddRange(await Discover(http, scanPorts, [], watchedInstances));
    if (apps.Count == 0)
        Console.WriteLine("none found yet — will keep scanning (start the app with options.EnableWebServer = true)");
}

Console.WriteLine($"XAML Updater: watching {(apps.Count > 0 ? string.Join(", ", apps) : "(waiting)")} → {src}{(dryRun ? "  (dry run)" : "")}");
Console.WriteLine("Enable the \"✎ XAML\" toggle in the web inspector to record edits. Ctrl+C to stop.");
var patcher = new XamlPatcher(src, dryRun);
var cursors = new Dictionary<string, long>();
var connectedApps = new HashSet<string>();

foreach (var url in apps)
    cursors[url] = fromNow ? await CurrentSeq(http, url) : 0;

var tick = 0;
var knownSerials = await AdbForwarder.SerialsSafe();
while (true)
{
    // The scan is cheap once apps answer; silent ports cost a short timeout each,
    // so a rescan every ~15 s keeps newly launched devices joining automatically.
    if (autoDiscover && tick % Math.Max(1, 15000 / intervalMs) == 0 && tick > 0)
    {
        // A new Android device needs its forwards before any port scan can see it —
        // the expensive probing runs only when the adb device list actually changes.
        var serials = await AdbForwarder.SerialsSafe();
        if (!serials.SetEquals(knownSerials))
        {
            knownSerials = serials;
            foreach (var forward in await AdbForwarder.EnsureForwards(http, scanPorts))
                Console.WriteLine($"adb forward: {forward.Serial} device port {forward.DevicePort} → http://localhost:{forward.HostPort}  ({forward.Device})");
        }

        foreach (var url in await Discover(http, scanPorts, apps, watchedInstances, quiet: true))
        {
            apps.Add(url);
            cursors[url] = fromNow ? await CurrentSeq(http, url) : 0;
            Console.WriteLine($"attached {url}");
        }
    }

    foreach (var url in apps)
    {
        try
        {
            var json = JsonNode.Parse(await http.GetStringAsync($"{url}/api/changes?since={cursors[url]}&caps=el"));
            if (connectedApps.Add(url))
                Console.WriteLine($"connected to {url}");

            foreach (var node in json?["changes"] as JsonArray ?? [])
            {
                if (node == null)
                    continue;

                var change = new XamlChange(
                    node["source"]?.GetValue<string>() ?? "",
                    node["line"]?.GetValue<int>() ?? 0,
                    node["column"]?.GetValue<int>() ?? 0,
                    node["element"]?.GetValue<string>() ?? "",
                    node["attribute"]?.GetValue<string>() ?? "",
                    node["value"]?.GetValue<string>() ?? "",
                    node["remove"]?.GetValue<bool>() ?? false,
                    node["op"]?.GetValue<string>() ?? "attr");

                patcher.Apply(change);
            }

            cursors[url] = json?["seq"]?.GetValue<long>() ?? cursors[url];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            if (connectedApps.Remove(url))
                Console.WriteLine($"{url} not reachable — waiting…");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error ({url}): {ex.Message}");
        }
    }

    await Task.Delay(intervalMs);
    tick++;
}

static async Task<long> CurrentSeq(HttpClient http, string url)
{
    try
    {
        var initial = JsonNode.Parse(await http.GetStringAsync($"{url}/api/changes?since=0&caps=el"));
        return initial?["seq"]?.GetValue<long>() ?? 0;
    }
    catch
    {
        return 0; // app not up yet — it will replay from the start once reachable
    }
}

static async Task<List<string>> Discover(
    HttpClient http, int[] ports, IReadOnlyCollection<string> known, HashSet<string> instances, bool quiet = false)
{
    var found = new List<string>();
    foreach (var port in ports)
    {
        var baseUrl = $"http://localhost:{port}";
        if (known.Contains(baseUrl))
            continue;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(quiet ? 300 : 700));
            var json = JsonNode.Parse(await http.GetStringAsync($"{baseUrl}/api/ping", cts.Token));
            var device = json?["device"]?.GetValue<string>() ?? "";
            var instance = json?["instance"]?.GetValue<string>() ?? baseUrl;
            if (!instances.Add(instance))
                continue; // same app already watched through another host port
            found.Add(baseUrl);
            if (!quiet)
                Console.WriteLine($"found inspector at {baseUrl}{(string.IsNullOrEmpty(device) ? "" : $"  ({device})")}");
        }
        catch
        {
            // nothing on this port
        }
    }
    return found;
}

