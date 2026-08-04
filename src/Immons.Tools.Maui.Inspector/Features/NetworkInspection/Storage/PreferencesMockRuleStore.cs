namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection.Storage;

/// <summary>
/// Default backend: the whole rule set lives in one Preferences entry. Simple and dependency-free,
/// but every write re-serialises all rules — see Immons.Tools.Maui.Inspector.Persistency for a
/// SQLite backend that writes one row at a time.
/// </summary>
internal sealed class PreferencesMockRuleStore : IMockRuleStore
{
    const string RulesKey = "hv_mock_rules";
    const string ActiveScenarioKey = "hv_mock_scenario";
    const string ScenarioListKey = "hv_mock_scenarios";
    const string MockingEnabledKey = "hv_mock_enabled";

    readonly object _gate = new();
    List<MockRule>? _rules;

    public IReadOnlyList<MockRule> LoadRules()
    {
        lock (_gate)
        {
            return _rules ??= Read();
        }
    }

    static List<MockRule> Read()
    {
        try
        {
            var json = Preferences.Default.Get<string?>(RulesKey, null);
            return string.IsNullOrEmpty(json) ? [] : MockRuleSerializer.ListFromJson(json).ToList();
        }
        catch
        {
            // Preferences unavailable (unit tests / neutral TFM) — start empty.
            return [];
        }
    }

    public void SaveRules(IReadOnlyList<MockRule> rules)
    {
        lock (_gate)
        {
            var all = _rules ??= Read();
            foreach (var rule in rules)
            {
                var index = all.FindIndex(r => r.Id == rule.Id);
                if (index < 0)
                    all.Add(rule);
                else
                    all[index] = rule;
            }
            Flush(all);
        }
    }

    public void DeleteRule(int id)
    {
        lock (_gate)
        {
            var all = _rules ??= Read();
            if (all.RemoveAll(r => r.Id == id) > 0)
                Flush(all);
        }
    }

    static void Flush(List<MockRule> rules)
    {
        try
        {
            Preferences.Default.Set(RulesKey, MockRuleSerializer.ListToJson(rules));
        }
        catch
        {
            // see Read
        }
    }

    public IReadOnlyList<string> LoadScenarios()
    {
        try
        {
            var stored = Preferences.Default.Get<string?>(ScenarioListKey, null);
            return string.IsNullOrEmpty(stored)
                ? []
                : stored.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }
        catch
        {
            return [];
        }
    }

    public void SaveScenarios(IReadOnlyList<string> names)
    {
        try
        {
            Preferences.Default.Set(ScenarioListKey, string.Join('\n', names));
        }
        catch
        {
            // see Read
        }
    }

    public bool LoadMockingEnabled()
    {
        try
        {
            // Absent means "never switched off" — mocking is on by default.
            return Preferences.Default.Get<string?>(MockingEnabledKey, null) != "0";
        }
        catch
        {
            return true;
        }
    }

    public void SaveMockingEnabled(bool enabled)
    {
        try
        {
            Preferences.Default.Set(MockingEnabledKey, enabled ? "1" : "0");
        }
        catch
        {
            // see Read
        }
    }

    public string LoadActiveScenario()
    {
        try
        {
            return Preferences.Default.Get<string?>(ActiveScenarioKey, null) ?? "";
        }
        catch
        {
            return "";
        }
    }

    public void SaveActiveScenario(string name)
    {
        try
        {
            Preferences.Default.Set(ActiveScenarioKey, name);
        }
        catch
        {
            // see Read
        }
    }
}
