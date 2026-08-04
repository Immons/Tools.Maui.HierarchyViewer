using Immons.Tools.Maui.Inspector.Features.Editing.Storage;
using Immons.Tools.Maui.Inspector.Persistency.Entities;
using SQLite;

namespace Immons.Tools.Maui.Inspector.Persistency;

/// <summary>Applied expressions as rows — one per edited property.</summary>
internal sealed class SqliteExpressionStore : IExpressionStore
{
    readonly SQLiteConnection _db;

    public SqliteExpressionStore(SQLiteConnection db) => _db = db;

    public string? Find(string key)
    {
        lock (_db)
        {
            return _db.Find<ExpressionRow>(key)?.Expression;
        }
    }

    public void Save(string key, string? expression)
    {
        lock (_db)
        {
            if (expression == null)
                _db.Delete<ExpressionRow>(key);
            else
                _db.InsertOrReplace(new ExpressionRow { Key = key, Expression = expression });
        }
    }
}
