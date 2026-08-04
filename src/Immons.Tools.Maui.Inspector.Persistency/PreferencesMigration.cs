using Immons.Tools.Maui.Inspector.Shared.Storage;

namespace Immons.Tools.Maui.Inspector.Persistency;

/// <summary>
/// One-time move of rules, scenarios and breakpoints from the default Preferences backend.
/// Runs only when the database has no rules yet, so it never fights with what SQLite already holds.
/// </summary>
internal static class PreferencesMigration
{
    // Applied expressions are keyed by an opaque hash and Preferences cannot be enumerated,
    // so they are not migrated — they are re-saved into SQLite the next time you apply an edit.
    static readonly string[] MigratedPreferenceKeys =
        ["hv_mock_rules", "hv_mock_scenario", "hv_mock_scenarios", "hv_breakpoints"];

    public static int Run(IInspectorStorage target, bool clearAfterwards)
    {
        try
        {
            if (target.MockRules.LoadRules().Count > 0)
                return 0;

            var source = new PreferencesInspectorStorage();
            var rules = source.MockRules.LoadRules();
            var scenarios = source.MockRules.LoadScenarios();
            var breakpoints = source.Breakpoints.Load();

            if (rules.Count > 0)
                target.MockRules.SaveRules(rules);
            if (scenarios.Count > 0)
                target.MockRules.SaveScenarios(scenarios);
            target.MockRules.SaveActiveScenario(source.MockRules.LoadActiveScenario());
            target.Breakpoints.Save(breakpoints);

            // Only drop the old copy once the new one is provably complete.
            if (clearAfterwards && target.MockRules.LoadRules().Count == rules.Count)
                ClearPreferences();

            return rules.Count;
        }
        catch
        {
            // A failed migration must never stop the app — the inspector just starts empty.
            return 0;
        }
    }

    static void ClearPreferences()
    {
        foreach (var key in MigratedPreferenceKeys)
        {
            try
            {
                Preferences.Default.Remove(key);
            }
            catch
            {
                // Preferences unavailable — nothing to clean up.
            }
        }
    }
}
