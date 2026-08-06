namespace Immons.Tools.Maui.Inspector.Features.Structure.Storage;

/// <summary>Preferences backend: structural edits stay session-only.</summary>
internal sealed class NoOpStructureStore : IStructureStore
{
    public IReadOnlyList<string> All() => [];

    public void Save(string id, string json)
    {
    }

    public void Delete(string id)
    {
    }
}
