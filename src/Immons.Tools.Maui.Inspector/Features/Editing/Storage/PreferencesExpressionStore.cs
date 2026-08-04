namespace Immons.Tools.Maui.Inspector.Features.Editing.Storage;

/// <summary>One Preferences entry per expression — small values, so a key/value store fits well.</summary>
internal sealed class PreferencesExpressionStore : IExpressionStore
{
    public string? Find(string key)
    {
        try
        {
            return Preferences.Default.Get<string?>(key, null);
        }
        catch
        {
            // Preferences unavailable (unit tests / neutral TFM) — the caller's cache still serves the session.
            return null;
        }
    }

    public void Save(string key, string? expression)
    {
        try
        {
            if (expression == null)
                Preferences.Default.Remove(key);
            else
                Preferences.Default.Set(key, expression);
        }
        catch
        {
            // see Find
        }
    }
}
