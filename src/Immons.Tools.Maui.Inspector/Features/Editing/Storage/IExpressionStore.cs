namespace Immons.Tools.Maui.Inspector.Features.Editing.Storage;

/// <summary>
/// Applied editor expressions ({Binding …}, {StaticResource …}, {OnPlatform …}) keyed by the
/// element's XAML identity, so they can be re-applied after a restart.
/// </summary>
internal interface IExpressionStore
{
    string? Find(string key);

    /// <summary>A null expression removes the entry.</summary>
    void Save(string key, string? expression);
}
