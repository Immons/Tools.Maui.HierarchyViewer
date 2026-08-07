using static Immons.Tools.Maui.Inspector.Features.Properties.SectionBuilder;

namespace Immons.Tools.Maui.Inspector.Features.Properties.Sections;

/// <summary>Editable row/column definitions of a Grid, with add/remove actions.</summary>
internal sealed class GridDefinitionsSectionBuilder(IXamlChangeLog xamlChanges) : IPropertySectionBuilder
{
    public IEnumerable<PropertySection> Build(InspectionContext context)
    {
        if (context.Element is not Grid grid)
            yield break;

        yield return BuildAxis(grid, rows: true);
        yield return BuildAxis(grid, rows: false);
    }

    PropertySection BuildAxis(Grid grid, bool rows)
    {
        var s = New(rows ? "Rows" : "Columns", group: "griddefs");

        var count = rows ? grid.RowDefinitions.Count : grid.ColumnDefinitions.Count;
        for (var i = 0; i < count; i++)
            AddDefinitionRow(s, grid, rows, i);

        if (count == 0)
            s.Rows.Add(new PropertyRow("", rows ? "(implicit single row)" : "(implicit single column)"));

        s.Rows.Add(new PropertyRow("", rows ? "＋ Add row (Auto)" : "＋ Add column (Auto)", Action: () =>
        {
            if (rows)
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            else
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            AfterDefinitionsChanged(grid, rows);
        }));

        if (count > 0)
        {
            s.Rows.Add(new PropertyRow("", rows ? "✕ Remove last row" : "✕ Remove last column", Action: () =>
            {
                if (rows && grid.RowDefinitions.Count > 0)
                    grid.RowDefinitions.RemoveAt(grid.RowDefinitions.Count - 1);
                else if (!rows && grid.ColumnDefinitions.Count > 0)
                    grid.ColumnDefinitions.RemoveAt(grid.ColumnDefinitions.Count - 1);
                AfterDefinitionsChanged(grid, rows);
            }));
        }

        return s;
    }

    void AddDefinitionRow(PropertySection s, Grid grid, bool rows, int index)
    {
        var definition = rows ? (BindableObject)grid.RowDefinitions[index] : grid.ColumnDefinitions[index];
        var attribute = rows ? "Height" : "Width";
        var editor = new PropertyEditor(EditorKind.Text, null, text =>
        {
            // "{OnIdiom …}" / "{OnPlatform …}" from the per-device editor: this device's
            // entry applies live, the whole expression goes to XAML and the ⋔ badge.
            var live = text;
            var isExpression = DeviceValueExpressionParser.TryResolve(text, out var deviceValue);
            if (isExpression)
            {
                if (deviceValue == null)
                {
                    // No entry for this device — a XAML-only edit, but the composed
                    // shorthand attribute must still be re-recorded.
                    InspectorServices.Current.Expressions.Record(definition, attribute, text.Trim());
                    AfterDefinitionsChanged(grid, rows);
                    return true;
                }
                live = deviceValue;
            }

            if (!GridLengthText.TryParse(live, out var length))
                return false;
            if (rows)
            {
                if (index >= grid.RowDefinitions.Count)
                    return false;
                grid.RowDefinitions[index].Height = length;
            }
            else
            {
                if (index >= grid.ColumnDefinitions.Count)
                    return false;
                grid.ColumnDefinitions[index].Width = length;
            }
            InspectorServices.Current.Expressions.Record(definition, attribute, isExpression ? text.Trim() : null);
            AfterDefinitionsChanged(grid, rows);
            return true;
        })
        {
            // Element-form <RowDefinition/> tags carry their own source info — patch them directly.
            XamlTarget = definition,
            XamlAttribute = attribute,
        };

        var current = rows ? grid.RowDefinitions[index].Height : grid.ColumnDefinitions[index].Width;
        s.Rows.Add(new PropertyRow(rows ? $"Row {index}" : $"Column {index}", GridLengthText.Format(current), null, editor,
            DeviceExpression: InspectorServices.Current.Expressions.Find(definition, attribute)));
    }

    /// <summary>
    /// "{OnIdiom Default='*,3*,*', Phone='0,*,0'}" from per-definition expressions: for every
    /// idiom/platform key used anywhere, the full definitions list is joined — expression-free
    /// definitions contribute their live value. Null when the expressions mix extension types.
    /// </summary>
    static string? ComposeDeviceExpression(IReadOnlyList<string> liveValues, IReadOnlyList<string?> expressions)
    {
        string? extension = null;
        var parsed = new List<Dictionary<string, string>?>();
        foreach (var expression in expressions)
        {
            if (expression == null)
            {
                parsed.Add(null);
                continue;
            }
            if (!DeviceValueExpressionParser.TryParseEntries(expression, out var kind, out var entries))
                return null;
            if (extension != null && extension != kind)
                return null; // OnIdiom and OnPlatform mixed across definitions
            extension = kind;
            parsed.Add(entries);
        }
        if (extension == null)
            return null;

        var keys = new List<string>();
        foreach (var entries in parsed)
        {
            foreach (var key in entries?.Keys ?? Enumerable.Empty<string>())
            {
                if (!key.Equals("Default", StringComparison.OrdinalIgnoreCase)
                    && !keys.Contains(key, StringComparer.OrdinalIgnoreCase))
                    keys.Add(key);
            }
        }

        string JoinFor(string? key) => string.Join(",", liveValues.Select((live, i) =>
        {
            var entries = parsed[i];
            if (entries == null)
                return live;
            if (key != null && entries.TryGetValue(key, out var forKey))
                return forKey;
            return entries.TryGetValue("Default", out var fallback) ? fallback : live;
        }));

        var parts = new List<string> { $"Default='{JoinFor(null)}'" };
        parts.AddRange(keys.Select(key => $"{key}='{JoinFor(key)}'"));
        return "{" + extension + " " + string.Join(", ", parts) + "}";
    }

    void AfterDefinitionsChanged(Grid grid, bool rows)
    {
        ((IView)grid).InvalidateMeasure();
        RecordDefinitions(grid, rows);
    }

    /// <summary>
    /// Writes grid definitions to XAML in the shorthand-attribute form (RowDefinitions="Auto,*").
    /// When the definitions were declared as elements they carry their own source info — the
    /// per-definition editors patch those directly and this method backs off to avoid duplicates
    /// (add/remove of element-form definitions is not written back).
    /// </summary>
    void RecordDefinitions(Grid grid, bool rows)
    {
        try
        {
            var elementForm = rows
                ? grid.RowDefinitions.Any(d => Microsoft.Maui.VisualDiagnostics.GetSourceInfo(d) != null)
                : grid.ColumnDefinitions.Any(d => Microsoft.Maui.VisualDiagnostics.GetSourceInfo(d) != null);
            if (elementForm)
                return;

            var attribute = rows ? "RowDefinitions" : "ColumnDefinitions";
            var lengthAttribute = rows ? "Height" : "Width";
            var definitions = rows
                ? grid.RowDefinitions.Cast<BindableObject>().ToList()
                : grid.ColumnDefinitions.Cast<BindableObject>().ToList();
            var liveValues = rows
                ? grid.RowDefinitions.Select(d => GridLengthText.Format(d.Height)).ToList()
                : grid.ColumnDefinitions.Select(d => GridLengthText.Format(d.Width)).ToList();
            var expressions = definitions
                .Select(d => InspectorServices.Current.Expressions.Find(d, lengthAttribute))
                .ToList();

            // A per-definition "{OnIdiom …}" cannot live inside the shorthand string — the
            // whole attribute becomes one expression with the joined list per idiom instead.
            var value = expressions.Any(e => e != null)
                ? ComposeDeviceExpression(liveValues, expressions) ?? string.Join(",", liveValues)
                : string.Join(",", liveValues);

            xamlChanges.Record(grid, attribute, value.Length == 0 ? XamlChangeLog.RemoveMarker : value);
        }
        catch
        {
            // diagnostics unavailable — skip source sync for this edit
        }
    }
}
