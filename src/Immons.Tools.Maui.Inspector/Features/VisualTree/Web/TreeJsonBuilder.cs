using System.Text.Json.Nodes;

namespace Immons.Tools.Maui.Inspector.Features.VisualTree.Web;

/// <summary>Visual tree + device/window header data as JSON.</summary>
internal sealed class TreeJsonBuilder(IActiveInspectorProvider inspectors, IElementRegistry elements) : ITreeJsonBuilder
{
    public string Build()
    {
        var root = new JsonObject();
        var rootsArray = new JsonArray();
        root["roots"] = rootsArray;
        root["device"] = DeviceDescription();

        if (inspectors.Current is { } inspector)
        {
            root["window"] = $"{ValueFormatter.F(inspector.WindowSize.Width)}×{ValueFormatter.F(inspector.WindowSize.Height)}";
            foreach (var node in TreeNode.Build(inspector.Roots))
                rootsArray.Add(ToJson(node));
        }

        return root.ToJsonString();
    }

    internal static string DeviceDescription()
    {
        try
        {
            var device = DeviceInfo.Current;
            var description = $"{device.Name} · {device.Platform} {device.VersionString}";
            if (device.DeviceType == DeviceType.Virtual)
                description += device.Platform == DevicePlatform.iOS || device.Platform == DevicePlatform.MacCatalyst
                    ? " · Simulator"
                    : " · Emulator";
            return description;
        }
        catch
        {
            return "";
        }
    }

    JsonObject ToJson(TreeNode node)
    {
        var obj = new JsonObject
        {
            ["id"] = elements.GetId(node.Element),
            ["label"] = node.Label,
            ["s"] = Searchable(node.Element, node.Label),
        };
        if (node.Children.Count > 0)
        {
            var children = new JsonArray();
            foreach (var child in node.Children)
                children.Add(ToJson(child));
            obj["children"] = children;
        }
        return obj;
    }

    /// <summary>Search haystack: label (truncated) + full text + ids, lowercased.</summary>
    static string Searchable(VisualElement element, string label)
    {
        var text = element switch
        {
            Label { FormattedText.Spans.Count: > 0 } l => string.Concat(l.FormattedText.Spans.Select(s => s.Text)),
            IText t => t.Text,
            _ => null,
        };
        return $"{label} {text} {element.StyleId} {element.AutomationId}".ToLowerInvariant();
    }
}
