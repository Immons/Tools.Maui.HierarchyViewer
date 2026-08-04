namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection.Storage;

/// <summary>
/// Durable home of mock rules and scenarios. Deliberately shaped around single rules rather than
/// one blob: a backend that can update one row turns "toggle a rule" into a write proportional to
/// that rule, not to the whole set (recorded scenarios reach megabytes).
/// </summary>
internal interface IMockRuleStore
{
    IReadOnlyList<MockRule> LoadRules();

    /// <summary>Inserts or updates exactly these rules; everything else is left alone.</summary>
    void SaveRules(IReadOnlyList<MockRule> rules);

    void DeleteRule(int id);

    IReadOnlyList<string> LoadScenarios();

    void SaveScenarios(IReadOnlyList<string> names);

    bool LoadMockingEnabled();

    void SaveMockingEnabled(bool enabled);

    string LoadActiveScenario();

    void SaveActiveScenario(string name);
}
