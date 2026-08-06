using Immons.Tools.Maui.Inspector.Features.Structure.Storage;
using Immons.Tools.Maui.Inspector.Persistency.Entities;
using SQLite;

namespace Immons.Tools.Maui.Inspector.Persistency;

/// <summary>Structural edits as rows — replayed on the next app start.</summary>
internal sealed class SqliteStructureStore : IStructureStore
{
    readonly SQLiteConnection _db;

    public SqliteStructureStore(SQLiteConnection db) => _db = db;

    public IReadOnlyList<string> All()
    {
        lock (_db)
        {
            return _db.Table<StructureOpRow>().Select(row => row.Json).ToList();
        }
    }

    public void Save(string id, string json)
    {
        lock (_db)
        {
            _db.InsertOrReplace(new StructureOpRow { Id = id, Json = json });
        }
    }

    public void Delete(string id)
    {
        lock (_db)
        {
            _db.Delete<StructureOpRow>(id);
        }
    }
}
