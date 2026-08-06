namespace Immons.Tools.Maui.Inspector.Features.History;

/// <summary>In-memory log of applied edits (old → new) with undo support for the web client.</summary>
internal interface IEditHistory
{
    long LastSeq { get; }

    void Record(VisualElement? element, string section, string name, string oldValue, string newValue, bool canUndo = true);

    EditHistory.Entry? Find(long seq);

    /// <summary>Flags an entry as undone: it leaves the Ctrl+Z chain, joins the redo stack.</summary>
    void MarkUndone(long seq);

    /// <summary>Puts a redone entry back into the Ctrl+Z chain.</summary>
    void MarkRedone(long seq);

    /// <summary>The most recently undone entry still eligible for redo; null when none.</summary>
    long? PopRedo();

    string ToJson();
}
