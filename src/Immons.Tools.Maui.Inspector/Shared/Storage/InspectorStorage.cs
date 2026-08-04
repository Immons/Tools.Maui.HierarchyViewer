namespace Immons.Tools.Maui.Inspector.Shared.Storage;

/// <summary>
/// The backend the inspector persists through. Preferences by default; an optional package swaps
/// it during app startup, before anything reads persisted state.
/// </summary>
internal static class InspectorStorage
{
    static IInspectorStorage _current = new PreferencesInspectorStorage();

    public static IInspectorStorage Current => _current;

    /// <summary>Raised after the backend changes so already-built registries can reload.</summary>
    public static event Action? Changed;

    public static void Use(IInspectorStorage storage)
    {
        _current = storage ?? throw new ArgumentNullException(nameof(storage));
        Changed?.Invoke();
    }
}
