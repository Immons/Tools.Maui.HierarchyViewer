namespace Immons.Tools.Maui.Inspector.Features.Structure;

/// <summary>Add/remove elements from the web client, with history and XAML write-back.</summary>
internal interface IStructureCommands
{
    /// <summary>Creates a catalog element under the parent; returns the new element's id.</summary>
    (int Id, string? Error) Add(int parentId, string typeName);

    /// <summary>
    /// Creates a catalog element at a window point (toolbox drop on the mirror): hit-tests the
    /// nearest container under the point and inserts at the matching child position.
    /// </summary>
    (int Id, string? Error) AddAt(Point windowPoint, string typeName);

    /// <summary>Container that would receive a drop at the point — its bounds and type name.</summary>
    (Rect Bounds, string Label, IReadOnlyList<Rect> Children)? DropTargetAt(Point windowPoint);

    /// <summary>Detaches the element; undo re-attaches the same instance.</summary>
    string? Remove(int elementId);

    /// <summary>Moves the element within its layout parent by delta positions (-1 = up, +1 = down).</summary>
    string? Move(int elementId, int delta);

    /// <summary>
    /// Moves the element under a different parent — next to the given sibling (0 = append).
    /// </summary>
    string? Reparent(int elementId, int newParentId, int siblingId, bool before);

    /// <summary>Wraps the element in a new container; returns the wrapper's id.</summary>
    (int Id, string? Error) Wrap(int elementId, string typeName);

    /// <summary>
    /// Pastes a deep copy of the source element into the target (or the target's nearest
    /// container ancestor when the target itself cannot take children).
    /// </summary>
    (int Id, string? Error) Paste(int targetId, int sourceId, bool force);

    /// <summary>
    /// Moves the chosen local property values into a keyed Style in the page's resources and
    /// re-points the element at it. Returns the element id for reselection.
    /// </summary>
    (int Id, string? Error) ExtractStyle(int elementId, string key, IReadOnlyCollection<string> propertyNames);

    /// <summary>Removes a container but keeps its children — they take its place in the parent.</summary>
    string? UnwrapElement(int elementId);

    /// <summary>Undo entry point for "Structure" history entries.</summary>
    bool Undo(long seq);

    /// <summary>Redo entry point for "Structure" history entries.</summary>
    bool Redo(long seq);
}
