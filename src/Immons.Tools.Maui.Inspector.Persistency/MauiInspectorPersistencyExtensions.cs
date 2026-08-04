using Immons.Tools.Maui.Inspector.Shared.Storage;

namespace Immons.Tools.Maui.Inspector.Persistency;

/// <summary>Opt-in SQLite storage for the inspector.</summary>
public static class MauiInspectorPersistencyExtensions
{
    /// <summary>
    /// Persists mock rules, scenarios, breakpoints and applied expressions in SQLite instead of
    /// Preferences. Call it next to <c>UseMauiInspector</c>, before the app touches stored state.
    /// Existing Preferences data is migrated on first run.
    /// </summary>
    /// <param name="builder">The app builder.</param>
    /// <param name="databasePath">Database file; defaults to <c>maui-inspector.db3</c> in the app data folder.</param>
    /// <param name="migrateFromPreferences">Move rules stored by an earlier run and drop the old copy. Default: true.</param>
    public static MauiAppBuilder UseMauiInspectorPersistency(
        this MauiAppBuilder builder,
        string? databasePath = null,
        bool migrateFromPreferences = true)
    {
        try
        {
            var storage = new SqliteInspectorStorage(databasePath ?? DefaultDatabasePath());
            if (migrateFromPreferences)
                PreferencesMigration.Run(storage, clearAfterwards: true);
            InspectorStorage.Use(storage);
        }
        catch
        {
            // Storage is a convenience, never a reason to fail startup — Preferences stays in charge.
        }
        return builder;
    }

    static string DefaultDatabasePath()
    {
        string folder;
        try
        {
            folder = FileSystem.AppDataDirectory;
        }
        catch
        {
            // Neutral TFM / unit tests — MAUI Essentials has no implementation there.
            folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "maui-inspector.db3");
    }
}
