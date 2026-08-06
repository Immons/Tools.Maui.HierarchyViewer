namespace Immons.Tools.Maui.Inspector.Features.Structure.Storage;

/// <summary>
/// Persisted structural edits (serialized <see cref="StructureOp"/> JSON), replayed on the next
/// app start. The Preferences backend deliberately stores nothing — structural edits are only
/// durable with the SQLite package (Immons.Tools.Maui.Inspector.Persistency).
/// </summary>
internal interface IStructureStore
{
    IReadOnlyList<string> All();

    void Save(string id, string json);

    void Delete(string id);
}
