namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

/// <summary>Registry of mock rules edited from the web panel.</summary>
internal interface IMockRules
{
    IReadOnlyList<MockRule> All { get; }

    /// <summary>Adds (Id == 0) or replaces (Id > 0) a rule; returns the stored rule.</summary>
    MockRule Save(MockRule rule);

    /// <summary>
    /// Applies a whole set at once — scenarios, rules and the active scenario — as a single write.
    /// Restoring a browser backup rule by rule is what made a large set trickle in visibly.
    /// </summary>
    void Import(IReadOnlyList<string> scenarios, string activeScenario, IReadOnlyList<MockRule> rules);

    bool Remove(int id);

    bool SetEnabled(int id, bool enabled);

    /// <summary>First enabled rule matching the call, or null.</summary>
    MockRule? Match(string method, string url);

    /// <summary>
    /// Master switch. When off, no rule matches — including global ones — so the app talks to the
    /// real API without anyone having to disable rules one by one. Rules are left untouched.
    /// </summary>
    bool MockingEnabled { get; }

    void SetMockingEnabled(bool enabled);

    /// <summary>Active scenario name; "" = global rules only.</summary>
    string ActiveScenario { get; }

    void SetActiveScenario(string scenario);

    /// <summary>Scenario registry — independent of rules; rules reference these by name.</summary>
    IReadOnlyList<string> ScenarioNames { get; }

    void AddScenario(string name);

    /// <summary>Removes the scenario and strips it from every rule (active resets when needed).</summary>
    void RemoveScenario(string name);
}
