using System.Text.Json.Nodes;
using Immons.Tools.Maui.Inspector.Sync;

string? app = null;
var src = Directory.GetCurrentDirectory();
var intervalMs = 1000;
var fromNow = false;
var dryRun = false;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--app" when i + 1 < args.Length:
            app = args[++i].TrimEnd('/');
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

                  --app        Base URL of the running app's web inspector (skips scanning).
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

if (app == null)
{
    int[] candidates = Enumerable.Range(9295, 15).ToArray();
    Console.WriteLine("scanning localhost for a running inspector…");
    app = await Discover(http, candidates);

    if (app == null && await TryAdbForward(candidates))
        app = await Discover(http, candidates);

    if (app == null)
    {
        app = "http://localhost:9295";
        Console.WriteLine($"none found yet — will keep trying {app} (start the app with options.EnableWebServer = true)");
    }
}

Console.WriteLine($"XAML Updater: watching {app} → {src}{(dryRun ? "  (dry run)" : "")}");
Console.WriteLine("Enable the \"✎ XAML\" toggle in the web inspector to record edits. Ctrl+C to stop.");
var patcher = new XamlPatcher(src, dryRun);
long since = 0;
var connected = false;

if (fromNow)
{
    try
    {
        var initial = JsonNode.Parse(await http.GetStringAsync($"{app}/api/changes?since=0&caps=el"));
        since = initial?["seq"]?.GetValue<long>() ?? 0;
    }
    catch
    {
        // app not up yet — fine, we'll catch up below
    }
}

while (true)
{
    try
    {
        var json = JsonNode.Parse(await http.GetStringAsync($"{app}/api/changes?since={since}&caps=el"));
        if (!connected)
        {
            connected = true;
            Console.WriteLine($"connected to {app}");
        }

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

        since = json?["seq"]?.GetValue<long>() ?? since;
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
        if (connected)
        {
            connected = false;
            Console.WriteLine("app not reachable — waiting…");
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"error: {ex.Message}");
    }

    await Task.Delay(intervalMs);
}

static async Task<string?> Discover(HttpClient http, int[] ports)
{
    var found = new List<(string Url, string Device)>();
    foreach (var port in ports)
    {
        var baseUrl = $"http://localhost:{port}";
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(700));
            var json = JsonNode.Parse(await http.GetStringAsync($"{baseUrl}/api/tree", cts.Token));
            found.Add((baseUrl, json?["device"]?.GetValue<string>() ?? ""));
        }
        catch
        {
            // nothing on this port
        }
    }

    if (found.Count == 0)
        return null;

    foreach (var (url, device) in found)
        Console.WriteLine($"found inspector at {url}{(string.IsNullOrEmpty(device) ? "" : $"  ({device})")}");
    if (found.Count > 1)
        Console.WriteLine($"multiple inspectors found — using {found[0].Url}; pass --app to pick another");

    return found[0].Url;
}

// Sets up adb port forwarding for Android emulators/devices; false when adb is unavailable.
static async Task<bool> TryAdbForward(int[] ports)
{
    try
    {
        foreach (var port in ports)
        {
            var psi = new System.Diagnostics.ProcessStartInfo("adb", $"forward tcp:{port} tcp:{port}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null)
                return false;
            await process.WaitForExitAsync();
        }
        Console.WriteLine("adb forward set up for ports 9295-9309");
        return true;
    }
    catch
    {
        return false; // adb not installed — iOS-only setup
    }
}
