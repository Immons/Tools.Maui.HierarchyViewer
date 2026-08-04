using System.Text.RegularExpressions;

namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

/// <summary>
/// One mock rule: method + URL pattern → actions applied to matching calls.
/// A pattern containing "*" is a wildcard match against the full URL; otherwise
/// a case-insensitive substring match.
/// </summary>
internal sealed record MockRule(
    int Id,
    bool Enabled,
    string Method,
    string UrlPattern,
    string? Name,
    int DelayMs,
    string FailMode,
    bool ShortCircuit,
    int? Status,
    string? RequestBody,
    string? ResponseBody,
    string[]? Scenarios = null)
{
    /// <summary>Scenario names this rule belongs to; empty = global rule.</summary>
    public string[] ScenarioList => Scenarios ?? [];

    /// <summary>Global rules always participate; scenario rules only when one of theirs is active.</summary>
    public bool AppliesIn(string activeScenario) =>
        Enabled && (ScenarioList.Length == 0 || ScenarioList.Contains(activeScenario));

    public const string FailNone = "";
    public const string FailTimeout = "timeout";
    public const string FailError = "error";

    public bool Matches(string method, string url)
    {
        if (!Enabled || UrlPattern.Length == 0)
            return false;
        if (Method != "*" && !Method.Equals(method, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!UrlPattern.Contains('*'))
            return url.Contains(UrlPattern, StringComparison.OrdinalIgnoreCase);

        var regex = "^" + Regex.Escape(UrlPattern).Replace(@"\*", ".*") + "$";
        return Regex.IsMatch(url, regex, RegexOptions.IgnoreCase);
    }

    /// <summary>Short badge shown next to matched entries in the Network tab.</summary>
    public string Tag => $"⚡ {(string.IsNullOrEmpty(Name) ? $"rule #{Id}" : Name)}";

    /// <summary>
    /// Literal characters in the pattern — the measure used to pick between overlapping rules:
    /// "*/plans/123" (11) outranks "*/plans/*" (8), so an id-specific rule wins over its wildcard.
    /// </summary>
    public int Specificity => UrlPattern.Count(c => c != '*');
}
