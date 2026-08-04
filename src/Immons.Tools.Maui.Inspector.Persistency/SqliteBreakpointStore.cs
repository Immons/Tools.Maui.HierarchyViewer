using Immons.Tools.Maui.Inspector.Features.NetworkInspection.Storage;
using Immons.Tools.Maui.Inspector.Persistency.Entities;
using SQLite;

namespace Immons.Tools.Maui.Inspector.Persistency;

/// <summary>Breakpoint configuration in the settings table.</summary>
internal sealed class SqliteBreakpointStore : IBreakpointStore
{
    const string RequestsKey = "breakpoints.requests";
    const string ResponsesKey = "breakpoints.responses";
    const string FilterKey = "breakpoints.filter";

    readonly SQLiteConnection _db;

    public SqliteBreakpointStore(SQLiteConnection db) => _db = db;

    public BreakpointSettings Load()
    {
        lock (_db)
        {
            return new BreakpointSettings(
                _db.Find<SettingRow>(RequestsKey)?.Value == "1",
                _db.Find<SettingRow>(ResponsesKey)?.Value == "1",
                _db.Find<SettingRow>(FilterKey)?.Value ?? "");
        }
    }

    public void Save(BreakpointSettings settings)
    {
        lock (_db)
        {
            _db.RunInTransaction(() =>
            {
                _db.InsertOrReplace(new SettingRow { Key = RequestsKey, Value = settings.CaptureRequests ? "1" : "0" });
                _db.InsertOrReplace(new SettingRow { Key = ResponsesKey, Value = settings.CaptureResponses ? "1" : "0" });
                _db.InsertOrReplace(new SettingRow { Key = FilterKey, Value = settings.UrlFilter });
            });
        }
    }
}
