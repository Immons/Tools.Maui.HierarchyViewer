namespace Immons.Tools.Maui.Inspector.Features.Properties.Web;

/// <summary>Finds the addressed row via the collector and runs its editor/action, recording history.</summary>
internal sealed class PropertyCommands(
    IActiveInspectorProvider inspectors,
    IElementRegistry elements,
    IPropertyCollector properties,
    IEditHistory history) : IPropertyCommands
{
    public bool Apply(int id, string section, string name, string value)
    {
        var row = FindRows(id, section)?.FirstOrDefault(r => r.Name == name && r.Editor != null);
        if (row?.Editor is not { } editor)
            return false;
        var ok = editor.Apply(value);
        if (ok)
        {
            history.Record(elements.Find(id), section, name, row.Value, value);
            inspectors.Current?.RemoteAfterEdit();
        }
        return ok;
    }

    public bool Clear(int id, string section, string name)
    {
        var row = FindRows(id, section)?.FirstOrDefault(r => r.Name == name && r.Editor is { CanClear: true });
        if (row?.Editor is not { } editor)
            return false;
        var ok = editor.Clear();
        if (ok)
        {
            history.Record(elements.Find(id), section, name, row.Value, "(cleared)");
            inspectors.Current?.RemoteAfterEdit();
        }
        return ok;
    }

    public bool RunAction(int id, string section, string label)
    {
        var row = FindRows(id, section)?.FirstOrDefault(r => r.Action != null && (r.Value == label || r.Name == label));
        if (row?.Action is not { } action)
            return false;
        action();
        history.Record(elements.Find(id), section, label, "", "(action)", canUndo: false);
        inspectors.Current?.RemoteAfterEdit();
        return true;
    }

    public bool Undo(long seq)
    {
        if (history.Find(seq) is not { CanUndo: true } entry)
            return false;

        var row = FindRows(entry.ElementId, entry.Section)?
            .FirstOrDefault(r => r.Name == entry.Name && r.Editor != null);
        if (row?.Editor is not { } editor)
            return false;

        var ok = (entry.OldValue.Length == 0 || entry.OldValue == "(cleared)") && editor.CanClear
            ? editor.Clear()
            : editor.Apply(entry.OldValue);

        if (ok)
        {
            history.Record(elements.Find(entry.ElementId), entry.Section, entry.Name, entry.NewValue, entry.OldValue);
            inspectors.Current?.RemoteAfterEdit();
        }
        return ok;
    }

    List<PropertyRow>? FindRows(int id, string sectionTitle)
    {
        if (inspectors.Current is not { } inspector || elements.Find(id) is not { } element)
            return null;

        var sections = properties.Collect(element, inspector.BoundsOf(element));
        return sections.FirstOrDefault(s => s.Title == sectionTitle)?.Rows;
    }
}
