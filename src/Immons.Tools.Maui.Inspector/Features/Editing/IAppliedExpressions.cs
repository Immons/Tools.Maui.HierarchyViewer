namespace Immons.Tools.Maui.Inspector.Features.Editing;

/// <summary>
/// Remembers the last OnPlatform/OnIdiom expression applied to a property, so the panel
/// can show and pre-fill it (the runtime itself only holds the resolved value).
/// </summary>
internal interface IAppliedExpressions
{
    /// <summary>Stores the expression; null clears it (a plain value was applied).</summary>
    void Record(object target, string property, string? expression);

    string? Find(object target, string property);
}
