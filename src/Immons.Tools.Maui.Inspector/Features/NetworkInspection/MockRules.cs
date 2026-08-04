using Immons.Tools.Maui.Inspector.Features.NetworkInspection.Storage;
using Immons.Tools.Maui.Inspector.Shared.Storage;

namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

/// <summary>
/// Thread-safe rule registry; first matching rule wins. Rules are held in memory for matching and
/// persisted through <see cref="IMockRuleStore"/>, so startup-time calls (version checks etc.) are
/// already mocked on the next app run.
/// </summary>
internal sealed class MockRules : IMockRules
{
    readonly object _gate = new();
    readonly List<MockRule> _rules = [];
    readonly List<string> _scenarios = [];
    int _nextId;
    volatile string _activeScenario = "";
    volatile bool _mockingEnabled = true;
    volatile bool _launchScenarioSettled;
    volatile bool _seedChecked;

    static IMockRuleStore Store => InspectorStorage.Current.MockRules;

    public MockRules()
    {
        Load();
        // A storage backend installed during startup replaces what was read from the default one.
        InspectorStorage.Changed += Load;
    }

    void Load()
    {
        lock (_gate)
        {
            _rules.Clear();
            _scenarios.Clear();
            _rules.AddRange(Store.LoadRules());
            _nextId = _rules.Count == 0 ? 0 : _rules.Max(r => r.Id);
            _activeScenario = Store.LoadActiveScenario();
            _mockingEnabled = Store.LoadMockingEnabled();
            _launchScenarioSettled = false;
            ApplyLaunchScenario();
            _scenarios.AddRange(Store.LoadScenarios());
            // Registry and rule tags can drift (legacy backups, old imports) — heal from tags.
            foreach (var rule in _rules)
                EnsureScenariosOf(rule);
        }
    }

    public bool MockingEnabled => _mockingEnabled;

    public void SetMockingEnabled(bool enabled)
    {
        _mockingEnabled = enabled;
        Store.SaveMockingEnabled(enabled);
    }

    public string ActiveScenario => _activeScenario;

    public void SetActiveScenario(string scenario)
    {
        _activeScenario = scenario.Trim();
        Store.SaveActiveScenario(_activeScenario);
    }

    public IReadOnlyList<string> ScenarioNames
    {
        get
        {
            lock (_gate)
            {
                return _scenarios.ToList();
            }
        }
    }

    public void AddScenario(string name)
    {
        name = name.Trim();
        if (name.Length == 0)
            return;
        lock (_gate)
        {
            if (!_scenarios.Contains(name))
            {
                _scenarios.Add(name);
                _scenarios.Sort(StringComparer.OrdinalIgnoreCase);
                PersistScenarios();
            }
        }
    }

    public void RemoveScenario(string name)
    {
        lock (_gate)
        {
            if (!_scenarios.Remove(name))
                return;
            var touched = new List<MockRule>();
            for (var i = 0; i < _rules.Count; i++)
            {
                if (_rules[i].ScenarioList.Contains(name))
                {
                    _rules[i] = _rules[i] with { Scenarios = _rules[i].ScenarioList.Where(s => s != name).ToArray() };
                    touched.Add(_rules[i]);
                }
            }
            PersistScenarios();
            if (touched.Count > 0)
                Store.SaveRules(touched);
        }
        if (_activeScenario == name)
            SetActiveScenario("");
    }

    void PersistScenarios() => Store.SaveScenarios(_scenarios.ToList());

    public IReadOnlyList<MockRule> All
    {
        get
        {
            // Also here, not just in Match: the panel (and a UI test) may ask what is loaded
            // before the app has made its first call.
            SeedRulesOnce();
            lock (_gate)
            {
                return _rules.ToList();
            }
        }
    }

    /// <summary>Adds any rule-referenced scenario missing from the registry (call inside the lock).</summary>
    void EnsureScenariosOf(MockRule rule)
    {
        var added = false;
        foreach (var name in rule.ScenarioList)
        {
            if (!_scenarios.Contains(name))
            {
                _scenarios.Add(name);
                added = true;
            }
        }
        if (added)
        {
            _scenarios.Sort(StringComparer.OrdinalIgnoreCase);
            PersistScenarios();
        }
    }

    public MockRule Save(MockRule rule)
    {
        lock (_gate)
        {
            EnsureScenariosOf(rule);
            if (rule.Id <= 0)
            {
                rule = rule with { Id = ++_nextId };
                _rules.Add(rule);
            }
            else
            {
                var index = _rules.FindIndex(r => r.Id == rule.Id);
                if (index < 0)
                    _rules.Add(rule);
                else
                    _rules[index] = rule;
            }
            Store.SaveRules([rule]);
            return rule;
        }
    }

    public void Import(IReadOnlyList<string> scenarios, string activeScenario, IReadOnlyList<MockRule> incoming)
    {
        lock (_gate)
        {
            foreach (var name in scenarios)
            {
                var trimmed = name.Trim();
                if (trimmed.Length > 0 && !_scenarios.Contains(trimmed))
                    _scenarios.Add(trimmed);
            }

            var stored = new List<MockRule>(incoming.Count);
            foreach (var rule in incoming)
            {
                var withId = rule.Id > 0 && _rules.All(r => r.Id != rule.Id) ? rule : rule with { Id = ++_nextId };
                var index = _rules.FindIndex(r => r.Id == withId.Id);
                if (index < 0)
                    _rules.Add(withId);
                else
                    _rules[index] = withId;
                EnsureScenariosOf(withId);
                stored.Add(withId);
            }

            _scenarios.Sort(StringComparer.OrdinalIgnoreCase);
            PersistScenarios();
            Store.SaveRules(stored);
        }
        SetActiveScenario(activeScenario);
    }

    public bool Remove(int id)
    {
        lock (_gate)
        {
            var removed = _rules.RemoveAll(r => r.Id == id) > 0;
            if (removed)
                Store.DeleteRule(id);
            return removed;
        }
    }

    public bool SetEnabled(int id, bool enabled)
    {
        lock (_gate)
        {
            var index = _rules.FindIndex(r => r.Id == id);
            if (index < 0)
                return false;
            _rules[index] = _rules[index] with { Enabled = enabled };
            Store.SaveRules([_rules[index]]);
            return true;
        }
    }

    /// <summary>
    /// Honours the launch argument the first time it can be read. Android only exposes intent
    /// extras once the activity exists, which is after this registry is built, so the lookup is
    /// retried until the platform can answer.
    /// </summary>
    void ApplyLaunchScenario()
    {
        if (_launchScenarioSettled)
            return;

        var requested = LaunchScenario.Requested();
        if (requested == null)
        {
            // Nothing passed: keep whatever was stored. Stop retrying once the platform is ready.
            _launchScenarioSettled = LaunchScenario.Resolvable;
            return;
        }

        _launchScenarioSettled = true;
        if (string.Equals(requested, LaunchScenario.Off, StringComparison.OrdinalIgnoreCase))
        {
            _mockingEnabled = false;
            return;
        }

        _mockingEnabled = true;
        _activeScenario = string.Equals(requested, LaunchScenario.None, StringComparison.OrdinalIgnoreCase)
            ? ""
            : requested;
    }

    /// <summary>Imports the bundled rule set when the app starts with an empty registry.</summary>
    void SeedRulesOnce()
    {
        if (_seedChecked || !LaunchScenario.Resolvable)
            return;
        _seedChecked = true;

        bool empty;
        lock (_gate)
        {
            empty = _rules.Count == 0;
        }
        if (!empty || RuleSeed.Source() is not { } source || RuleSeed.Read(source) is not { } json)
            return;

        RuleSeed.Apply(this, json);
    }

    public MockRule? Match(string method, string url)
    {
        ApplyLaunchScenario();
        SeedRulesOnce();

        if (!_mockingEnabled)
            return null;

        var scenario = _activeScenario;
        lock (_gate)
        {
            // Most specific pattern wins (id-rule over its wildcard); a scenario rule beats a
            // global one on ties (that's the point of activating it); exact method beats "*";
            // insertion order only breaks full ties.
            return _rules
                .Where(r => r.AppliesIn(scenario) && r.Matches(method, url))
                .OrderByDescending(r => r.Specificity)
                .ThenByDescending(r => r.ScenarioList.Length > 0)
                .ThenByDescending(r => r.Method != "*")
                .FirstOrDefault();
        }
    }
}
