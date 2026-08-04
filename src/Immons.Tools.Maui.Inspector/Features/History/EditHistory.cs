using System.Text.Json.Nodes;

namespace Immons.Tools.Maui.Inspector.Features.History;

/// <summary>Ring buffer of applied edits, exposed to the web client's History tab.</summary>
internal sealed class EditHistory(IElementRegistry elements) : IEditHistory
{
    internal sealed record Entry(
        long Seq,
        string Time,
        int ElementId,
        string ElementLabel,
        string Section,
        string Name,
        string OldValue,
        string NewValue,
        bool CanUndo);

    readonly RingLog<Entry> _log = new(limit: 300);

    public long LastSeq => _log.LastSeq;

    public void Record(VisualElement? element, string section, string name, string oldValue, string newValue, bool canUndo = true)
    {
        if (element == null || oldValue == newValue)
            return;

        var label = ElementInfo.ShortLabel(element);
        var id = elements.GetId(element);
        _log.Add(seq => new Entry(seq, DateTime.Now.ToString("HH:mm:ss"),
            id, label, section, name, oldValue, newValue, canUndo));
    }

    public Entry? Find(long seq) => _log.Find(e => e.Seq == seq);

    public string ToJson()
    {
        var array = new JsonArray();
        foreach (var e in _log.NewestFirst())
        {
            array.Add(new JsonObject
            {
                ["seq"] = e.Seq,
                ["time"] = e.Time,
                ["elementId"] = e.ElementId,
                ["element"] = e.ElementLabel,
                ["section"] = e.Section,
                ["name"] = e.Name,
                ["old"] = e.OldValue,
                ["new"] = e.NewValue,
                ["canUndo"] = e.CanUndo,
            });
        }
        return new JsonObject { ["entries"] = array }.ToJsonString();
    }
}
