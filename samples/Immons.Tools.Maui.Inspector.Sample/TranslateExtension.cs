namespace SampleApp;

/// <summary>
/// Stand-in for a typical localization extension ({extensions:Translate Key}) — the inspector
/// must resolve markup like this instead of writing the raw "{…}" text into the property.
/// </summary>
[ContentProperty(nameof(Key))]
public sealed class TranslateExtension : IMarkupExtension<string>
{
    static readonly Dictionary<string, string> Strings = new()
    {
        ["Greeting"] = "Hello from a markup extension",
        ["CollapseAll"] = "Collapse all",
        ["ExpandAll"] = "Expand all",
    };

    public string Key { get; set; } = "";

    public string ProvideValue(IServiceProvider serviceProvider) =>
        Strings.TryGetValue(Key, out var value) ? value : $"!{Key}!";

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) =>
        ProvideValue(serviceProvider);
}
