using System.Text.Json.Nodes;

namespace Immons.Tools.Maui.Inspector.Features.NetworkInspection;

/// <summary>Rule ↔ JSON, shared by the web endpoint and device-side persistence.</summary>
internal static class MockRuleSerializer
{
    public static JsonObject ToJson(MockRule r) => new()
    {
        ["id"] = r.Id,
        ["enabled"] = r.Enabled,
        ["method"] = r.Method,
        ["urlPattern"] = r.UrlPattern,
        ["name"] = r.Name,
        ["delayMs"] = r.DelayMs,
        ["failMode"] = r.FailMode,
        ["shortCircuit"] = r.ShortCircuit,
        ["status"] = r.Status,
        ["requestBody"] = r.RequestBody,
        ["responseBody"] = r.ResponseBody,
        ["scenarios"] = new JsonArray(r.ScenarioList.Select(n => (JsonNode)n!).ToArray()),
    };

    public static MockRule FromJson(JsonNode? node) => new(
        Id: (int?)node?["id"] ?? 0,
        Enabled: (bool?)node?["enabled"] ?? true,
        Method: (string?)node?["method"] ?? "*",
        UrlPattern: (string?)node?["urlPattern"] ?? "",
        Name: (string?)node?["name"],
        DelayMs: (int?)node?["delayMs"] ?? 0,
        FailMode: (string?)node?["failMode"] ?? MockRule.FailNone,
        ShortCircuit: (bool?)node?["shortCircuit"] ?? false,
        Status: (int?)node?["status"],
        RequestBody: NullIfEmpty((string?)node?["requestBody"]),
        ResponseBody: NullIfEmpty((string?)node?["responseBody"]),
        Scenarios: ReadScenarios(node));

    public static JsonArray ToJsonArray(IEnumerable<MockRule> rules)
    {
        var array = new JsonArray();
        foreach (var rule in rules)
            array.Add(ToJson(rule));
        return array;
    }

    public static string ListToJson(IEnumerable<MockRule> rules) => ToJsonArray(rules).ToJsonString();

    public static List<MockRule> ListFromJson(string json)
    {
        var rules = new List<MockRule>();
        if (JsonNode.Parse(json) is not JsonArray array)
            return rules;
        foreach (var node in array)
            rules.Add(FromJson(node));
        return rules;
    }

    static string[] ReadScenarios(JsonNode? node)
    {
        if (node?["scenarios"] is JsonArray array)
            return array.Select(n => ((string?)n ?? "").Trim()).Where(s => s.Length > 0).ToArray();
        // Legacy single-scenario field.
        var legacy = ((string?)node?["scenario"] ?? "").Trim();
        return legacy.Length == 0 ? [] : [legacy];
    }

    static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;
}
