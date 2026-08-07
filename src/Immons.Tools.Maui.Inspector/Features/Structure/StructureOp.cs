using System.Text.Json;
using System.Text.Json.Nodes;

namespace Immons.Tools.Maui.Inspector.Features.Structure;

/// <summary>
/// One persisted structural edit. Adds are keyed by the parent's XAML identity and carry the
/// created element's type plus every attribute edited so far; removes are keyed by the removed
/// element's own identity. Identities are "sourceUri:line:column" (see XamlSync.XamlSource).
/// </summary>
internal sealed record StructureOp(
    string Id,
    string Kind, // "add" | "remove" | "move"
    string? ParentIdentity,
    string ParentType,
    string TypeName,
    string Assembly,
    string ElementType,
    string? ElementIdentity,
    Dictionary<string, string> Attributes,
    string? SiblingIdentity = null,
    string SiblingType = "",
    string? SiblingOpId = null,
    bool Before = false,
    long Order = 0,
    string? SnippetXml = null,
    Dictionary<string, string>? SnippetXmlns = null,
    bool DeepCopy = false,
    string? StyleKey = null)
{
    public const string KindAdd = "add";
    public const string KindRemove = "remove";
    public const string KindMove = "move";
    public const string KindReparent = "reparent";
    public const string KindWrap = "wrap";
    public const string KindUnwrap = "unwrap";
    public const string KindStyle = "style";

    public string ToJson()
    {
        var attrs = new JsonObject();
        foreach (var (key, value) in Attributes)
            attrs[key] = value;

        return new JsonObject
        {
            ["id"] = Id,
            ["kind"] = Kind,
            ["parentIdentity"] = ParentIdentity,
            ["parentType"] = ParentType,
            ["type"] = TypeName,
            ["asm"] = Assembly,
            ["elementType"] = ElementType,
            ["elementIdentity"] = ElementIdentity,
            ["attrs"] = attrs,
            ["siblingIdentity"] = SiblingIdentity,
            ["siblingType"] = SiblingType,
            ["siblingOpId"] = SiblingOpId,
            ["before"] = Before,
            ["order"] = Order,
            ["snippetXml"] = SnippetXml,
            ["deepCopy"] = DeepCopy,
            ["styleKey"] = StyleKey,
            ["snippetXmlns"] = SnippetXmlns == null
                ? null
                : new JsonObject(SnippetXmlns.Select(kv => KeyValuePair.Create(kv.Key, (JsonNode?)kv.Value))),
        }.ToJsonString();
    }

    public static StructureOp? FromJson(string json)
    {
        try
        {
            if (JsonNode.Parse(json) is not JsonObject node)
                return null;

            var attrs = new Dictionary<string, string>();
            if (node["attrs"] is JsonObject attrsNode)
                foreach (var (key, value) in attrsNode)
                    if (value != null)
                        attrs[key] = value.GetValue<string>();

            return new StructureOp(
                node["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString("N"),
                node["kind"]?.GetValue<string>() ?? KindAdd,
                node["parentIdentity"]?.GetValue<string>(),
                node["parentType"]?.GetValue<string>() ?? "",
                node["type"]?.GetValue<string>() ?? "",
                node["asm"]?.GetValue<string>() ?? "",
                node["elementType"]?.GetValue<string>() ?? "",
                node["elementIdentity"]?.GetValue<string>(),
                attrs,
                node["siblingIdentity"]?.GetValue<string>(),
                node["siblingType"]?.GetValue<string>() ?? "",
                node["siblingOpId"]?.GetValue<string>(),
                node["before"]?.GetValue<bool>() ?? false,
                node["order"]?.GetValue<long>() ?? 0,
                node["snippetXml"]?.GetValue<string>(),
                node["snippetXmlns"] is JsonObject xmlnsNode
                    ? xmlnsNode.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value!.GetValue<string>())
                    : null,
                node["deepCopy"]?.GetValue<bool>() ?? false,
                node["styleKey"]?.GetValue<string>());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Splits "uri:line:column" from the right — the uri itself may contain colons.</summary>
    public static (string Uri, int Line, int Column)? ParseIdentity(string? identity)
    {
        if (string.IsNullOrEmpty(identity))
            return null;

        var lastColon = identity.LastIndexOf(':');
        var middleColon = lastColon > 0 ? identity.LastIndexOf(':', lastColon - 1) : -1;
        if (middleColon <= 0
            || !int.TryParse(identity[(middleColon + 1)..lastColon], out var line)
            || !int.TryParse(identity[(lastColon + 1)..], out var column))
            return null;

        return (identity[..middleColon], line, column);
    }
}
