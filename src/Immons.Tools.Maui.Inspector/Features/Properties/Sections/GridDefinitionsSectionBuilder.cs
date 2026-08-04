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
        var editor = new PropertyEditor(EditorKind.Text, null, text =>
        {
            if (!GridLengthText.TryParse(text, out var length))
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
            AfterDefinitionsChanged(grid, rows);
            return true;
        })
        {
            // Element-form <RowDefinition/> tags carry their own source info — patch them directly.
            XamlTarget = definition,
            XamlAttribute = rows ? "Height" : "Width",
        };

        var current = rows ? grid.RowDefinitions[index].Height : grid.ColumnDefinitions[index].Width;
        s.Rows.Add(new PropertyRow(rows ? $"Row {index}" : $"Column {index}", GridLengthText.Format(current), null, editor));
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
            var value = rows
                ? string.Join(",", grid.RowDefinitions.Select(d => GridLengthText.Format(d.Height)))
                : string.Join(",", grid.ColumnDefinitions.Select(d => GridLengthText.Format(d.Width)));

            xamlChanges.Record(grid, attribute, value.Length == 0 ? XamlChangeLog.RemoveMarker : value);
        }
        catch
        {
            // diagnostics unavailable — skip source sync for this edit
        }
    }
}
