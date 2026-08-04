namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

/// <summary>Last response per (method, URL) wins — the scenario snapshots the path's end state.</summary>
internal sealed class ScenarioRecorder(IMockRules rules) : IScenarioRecorder
{
    sealed record Captured(string Method, string Url, int Status, string Body);

    readonly object _gate = new();
    readonly Dictionary<string, Captured> _captured = [];
    volatile bool _recording;

    public bool Recording => _recording;

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _captured.Count;
            }
        }
    }

    public void Start()
    {
        lock (_gate)
        {
            _captured.Clear();
        }
        _recording = true;
    }

    public void Capture(string method, string url, int status, string? responseBody)
    {
        if (!_recording || responseBody == null)
            return; // binary/unreadable bodies cannot be replayed as text mocks

        lock (_gate)
        {
            _captured[$"{method} {url}"] = new Captured(method, url, status, responseBody);
        }
    }

    public int StopAndSave(string scenarioName)
    {
        _recording = false;
        List<Captured> captured;
        lock (_gate)
        {
            captured = _captured.Values.ToList();
            _captured.Clear();
        }

        if (captured.Count == 0)
            return 0;

        rules.AddScenario(scenarioName);
        foreach (var call in captured)
        {
            rules.Save(new MockRule(
                Id: 0,
                Enabled: true,
                Method: call.Method,
                UrlPattern: call.Url,
                Name: RuleNameFor(call.Url),
                DelayMs: 0,
                FailMode: MockRule.FailNone,
                ShortCircuit: true,
                Status: call.Status,
                RequestBody: null,
                ResponseBody: call.Body,
                Scenarios: [scenarioName]));
        }
        return captured.Count;
    }

    public void Cancel()
    {
        _recording = false;
        lock (_gate)
        {
            _captured.Clear();
        }
    }

    static string RuleNameFor(string url)
    {
        try
        {
            var path = new Uri(url).AbsolutePath.Trim('/');
            return path.Length == 0 ? url : path;
        }
        catch
        {
            return url;
        }
    }
}
