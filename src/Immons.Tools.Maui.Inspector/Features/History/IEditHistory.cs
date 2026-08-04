namespace Immons.Tools.Maui.Inspector.Features.History;

/// <summary>In-memory log of applied edits (old → new) with undo support for the web client.</summary>
internal interface IEditHistory
{
    long LastSeq { get; }

    void Record(VisualElement? element, string section, string name, string oldValue, string newValue, bool canUndo = true);

    EditHistory.Entry? Find(long seq);

    string ToJson();
}
