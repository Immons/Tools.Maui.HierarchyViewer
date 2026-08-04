using Immons.Tools.Maui.Inspector.Features.NetworkInspection;
using Immons.Tools.Maui.Inspector.Features.NetworkInspection.Storage;
using Immons.Tools.Maui.Inspector.Persistency.Entities;
using SQLite;

namespace Immons.Tools.Maui.Inspector.Persistency;

/// <summary>
/// Rules as rows: saving one rule is a single upsert regardless of how many are stored, which is
/// where this backend earns its keep — recorded scenarios routinely hold megabytes of bodies.
/// </summary>
internal sealed class SqliteMockRuleStore : IMockRuleStore
{
    const string ActiveScenarioKey = "mock.scenario.active";
    const string ScenarioListKey = "mock.scenario.list";
    const string MockingEnabledKey = "mock.enabled";

    readonly SQLiteConnection _db;

    public SqliteMockRuleStore(SQLiteConnection db) => _db = db;

    public IReadOnlyList<MockRule> LoadRules()
    {
        lock (_db)
        {
            return _db.Table<MockRuleRow>().ToList().Select(ToRule).ToList();
        }
    }

    public void SaveRules(IReadOnlyList<MockRule> rules)
    {
        if (rules.Count == 0)
            return;
        lock (_db)
        {
            _db.RunInTransaction(() =>
            {
                foreach (var rule in rules)
                    _db.InsertOrReplace(ToRow(rule));
            });
        }
    }

    public void DeleteRule(int id)
    {
        lock (_db)
        {
            _db.Delete<MockRuleRow>(id);
        }
    }

    public IReadOnlyList<string> LoadScenarios()
    {
        var stored = Setting(ScenarioListKey);
        return string.IsNullOrEmpty(stored) ? [] : stored.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    public void SaveScenarios(IReadOnlyList<string> names) =>
        SetSetting(ScenarioListKey, string.Join('\n', names));

    // Absent means "never switched off" — mocking is on by default.
    public bool LoadMockingEnabled() => Setting(MockingEnabledKey) != "0";

    public void SaveMockingEnabled(bool enabled) => SetSetting(MockingEnabledKey, enabled ? "1" : "0");

    public string LoadActiveScenario() => Setting(ActiveScenarioKey) ?? "";

    public void SaveActiveScenario(string name) => SetSetting(ActiveScenarioKey, name);

    string? Setting(string key)
    {
        lock (_db)
        {
            return _db.Find<SettingRow>(key)?.Value;
        }
    }

    void SetSetting(string key, string? value)
    {
        lock (_db)
        {
            _db.InsertOrReplace(new SettingRow { Key = key, Value = value });
        }
    }

    static MockRule ToRule(MockRuleRow row) => new(
        row.Id, row.Enabled, row.Method, row.UrlPattern, row.Name, row.DelayMs, row.FailMode,
        row.ShortCircuit, row.Status, row.RequestBody, row.ResponseBody,
        string.IsNullOrEmpty(row.Scenarios) ? null : row.Scenarios.Split('\n', StringSplitOptions.RemoveEmptyEntries));

    static MockRuleRow ToRow(MockRule rule) => new()
    {
        Id = rule.Id,
        Enabled = rule.Enabled,
        Method = rule.Method,
        UrlPattern = rule.UrlPattern,
        Name = rule.Name,
        DelayMs = rule.DelayMs,
        FailMode = rule.FailMode,
        ShortCircuit = rule.ShortCircuit,
        Status = rule.Status,
        RequestBody = rule.RequestBody,
        ResponseBody = rule.ResponseBody,
        Scenarios = rule.ScenarioList.Length == 0 ? null : string.Join('\n', rule.ScenarioList),
    };
}
