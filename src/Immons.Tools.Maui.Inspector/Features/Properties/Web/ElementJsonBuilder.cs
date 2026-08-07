using System.Text.Json.Nodes;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Web;

/// <summary>Property sections, XAML source and layout-explorer geometry of one element.</summary>
internal sealed class ElementJsonBuilder(
    IActiveInspectorProvider inspectors,
    IElementRegistry elements,
    IPropertyCollector properties) : IElementJsonBuilder
{
    public string? Build(int id)
    {
        if (inspectors.Current is not { } inspector || elements.Find(id) is not { } element)
            return null;

        var sections = properties.Collect(element, inspector.BoundsOf(element));
        var result = new JsonObject
        {
            ["id"] = id,
            ["type"] = element.GetType().Name,
            ["elementName"] = element.StyleId ?? "",
            ["automationId"] = element.AutomationId ?? "",
            ["page"] = PageIdentity.Of(element),
            ["templated"] = AutomationIdBinder.IsTemplatedItem(element),
        };

        if (XamlSource.Describe(element) is { } sourceInfo)
            result["source"] = sourceInfo;

        AddLayoutExplorer(result, inspector, element);

        var sectionsArray = new JsonArray();
        foreach (var section in sections)
        {
            if (BuildSection(section) is { } obj)
                sectionsArray.Add(obj);
        }

        result["sections"] = sectionsArray;
        return result.ToJsonString();
    }

    static JsonObject? BuildSection(PropertySection section)
    {
        var rows = new JsonArray();
        foreach (var row in section.Rows)
        {
            if (row.TogglesGroup != null)
                continue; // the web UI always shows grouped sections

            rows.Add(BuildRow(row));
        }

        if (rows.Count == 0)
            return null; // e.g. a section holding only a web-irrelevant toggle row

        return new JsonObject
        {
            ["title"] = section.Title,
            ["group"] = section.Group,
            ["rows"] = rows,
        };
    }

    static JsonObject BuildRow(PropertyRow row)
    {
        // Action rows are identified by their label (their Name is usually empty).
        var rowObj = new JsonObject
        {
            ["name"] = row.Action != null ? row.Value : row.Name,
            ["value"] = row.Action != null ? "" : row.Value,
        };
        if (row.Swatch != null)
            rowObj["swatch"] = ValueFormatter.Format(row.Swatch);
        if (row.Binding != null)
            rowObj["binding"] = row.Binding;
        if (row.DeviceExpression != null)
            rowObj["expr"] = row.DeviceExpression;
        if (row.Resources is { Count: > 0 } resources)
            rowObj["resources"] = new JsonArray(resources.Select(r => (JsonNode)r!).ToArray());
        if (row.Note != null)
            rowObj["note"] = row.Note;
        if (row.Action != null)
            rowObj["isAction"] = true;
        if (row.Editor is { } editor)
        {
            rowObj["kind"] = editor.Kind.ToString();
            if (editor.Choices is { } choices)
                rowObj["choices"] = new JsonArray(choices.Select(c => (JsonNode)c!).ToArray());
            if (editor.CanClear)
                rowObj["clearable"] = true;
        }
        return rowObj;
    }

    /// <summary>Geometry of the element's children for the web Layout Explorer.</summary>
    void AddLayoutExplorer(JsonObject result, IWindowInspector inspector, VisualElement element)
    {
        if (inspector.BoundsOf(element) is not { } bounds || bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var children = VisualTreeWalker.GetVisualChildren(element).ToList();
        if (children.Count == 0)
            return;

        var childArray = new JsonArray();
        foreach (var child in children)
        {
            if (inspector.BoundsOf(child) is not { } cb)
                continue;

            var obj = new JsonObject
            {
                ["id"] = elements.GetId(child),
                ["label"] = ElementInfo.ShortLabel(child),
                ["x"] = Math.Round(cb.X - bounds.X, 1),
                ["y"] = Math.Round(cb.Y - bounds.Y, 1),
                ["w"] = Math.Round(cb.Width, 1),
                ["h"] = Math.Round(cb.Height, 1),
            };
            if (element is Grid && child is View view)
            {
                obj["cell"] = $"r{Grid.GetRow(view)} c{Grid.GetColumn(view)}"
                    + (Grid.GetRowSpan(view) > 1 ? $" rs{Grid.GetRowSpan(view)}" : "")
                    + (Grid.GetColumnSpan(view) > 1 ? $" cs{Grid.GetColumnSpan(view)}" : "");
            }
            childArray.Add(obj);
        }

        if (childArray.Count == 0)
            return;

        var kind = element.GetType().Name;
        if (element is Grid grid)
            kind += $" {Math.Max(grid.RowDefinitions.Count, 1)}×{Math.Max(grid.ColumnDefinitions.Count, 1)}";

        result["layout"] = new JsonObject
        {
            ["kind"] = kind,
            ["w"] = Math.Round(bounds.Width, 1),
            ["h"] = Math.Round(bounds.Height, 1),
            ["children"] = childArray,
        };
    }
}
