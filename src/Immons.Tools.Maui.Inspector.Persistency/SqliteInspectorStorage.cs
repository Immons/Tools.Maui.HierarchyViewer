using Immons.Tools.Maui.Inspector.Features.Editing.Storage;
using Immons.Tools.Maui.Inspector.Features.NetworkInspection.Storage;
using Immons.Tools.Maui.Inspector.Features.Structure.Storage;
using Immons.Tools.Maui.Inspector.Persistency.Entities;
using Immons.Tools.Maui.Inspector.Shared.Storage;
using SQLite;

namespace Immons.Tools.Maui.Inspector.Persistency;

/// <summary>SQLite-backed storage for everything the inspector persists.</summary>
internal sealed class SqliteInspectorStorage : IInspectorStorage, IDisposable
{
    readonly SQLiteConnection _db;

    public SqliteInspectorStorage(string databasePath)
    {
        _db = new SQLiteConnection(databasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
        // WAL keeps a rule save from blocking a concurrent read on the web server's thread.
        _db.EnableWriteAheadLogging();
        _db.CreateTable<MockRuleRow>();
        _db.CreateTable<SettingRow>();
        _db.CreateTable<ExpressionRow>();
        _db.CreateTable<StructureOpRow>();

        MockRules = new SqliteMockRuleStore(_db);
        Breakpoints = new SqliteBreakpointStore(_db);
        Expressions = new SqliteExpressionStore(_db);
        Structure = new SqliteStructureStore(_db);
    }

    public IMockRuleStore MockRules { get; }

    public IBreakpointStore Breakpoints { get; }

    public IExpressionStore Expressions { get; }

    public IStructureStore Structure { get; }

    public string DatabasePath => _db.DatabasePath;

    public void Dispose() => _db.Close();
}
