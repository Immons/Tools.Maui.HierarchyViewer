using System.Text.Json.Nodes;
using Immons.Tools.Maui.Inspector.Shared;

namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

/// <summary>
/// Loads a rule set exported from the panel into an app that starts with none — the case a UI test
/// hits on every run with <c>clearState</c>. The file travels inside the app package, so no network
/// call has to win a race against the app's own startup requests.
/// </summary>
internal static class RuleSeed
{
    /// <summary>Launch argument naming the file, so one build can carry several rule sets.</summary>
    public const string ArgumentName = "inspectorRules";

    /// <summary>File name of a <c>MauiAsset</c> in the app package, or an absolute path on disk.</summary>
    public static string? Source() =>
        Empty(LaunchArguments.Value(ArgumentName)) ?? Empty(MauiInspector.Options.SeedRulesAsset);

    static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string? Read(string source)
    {
        try
        {
            if (Path.IsPathRooted(source) && File.Exists(source))
                return File.ReadAllText(source);

            using var stream = FileSystem.OpenAppPackageFileAsync(source).GetAwaiter().GetResult();
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch
        {
            // Missing or unreadable seed must never break startup — the app just has no rules.
            return null;
        }
    }

    /// <summary>Applies the panel's export format: { scenarios, activeScenario, rules }.</summary>
    public static int Apply(IMockRules rules, string json)
    {
        var root = JsonNode.Parse(json) as JsonObject;
        var incoming = (root?["rules"] as JsonArray ?? [])
            .Select(MockRuleSerializer.FromJson)
            .Select(r => r with { Id = 0 })
            .ToList();
        var scenarios = (root?["scenarios"] as JsonArray ?? [])
            .Select(n => (string?)n ?? "")
            .Where(n => n.Length > 0)
            .ToList();

        // The launch argument outranks the file: a UI test says which scenario it wants, and the
        // exported file merely carries whatever was selected when someone hit Export.
        var fromFile = (string?)root?["activeScenario"];
        var active = LaunchScenario.Requested() != null || string.IsNullOrEmpty(fromFile)
            ? rules.ActiveScenario
            : fromFile;

        rules.Import(scenarios, active, incoming);
        return incoming.Count;
    }
}
