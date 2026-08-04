namespace Immons.Tools.Maui.Inspector.Features.Properties;

/// <summary>Display and editing of ImageSource values as plain strings.</summary>
internal static class ImageSourceSupport
{
    public static string Text(ImageSource? source) => source switch
    {
        null => "",
        FileImageSource f => f.File ?? "",
        UriImageSource u => u.Uri?.ToString() ?? "",
        _ => Describe(source),
    };

    public static string Describe(ImageSource? source) => source switch
    {
        null => "–",
        FileImageSource f => $"File: {f.File}",
        UriImageSource u => $"Uri: {u.Uri}",
        FontImageSource fo => $"Font glyph: {fo.Glyph} ({fo.FontFamily})",
        StreamImageSource => "Stream",
        _ => source.GetType().Name,
    };

    /// <summary>Editor accepting a plain string: http(s) URLs become UriImageSource, anything else a file source.</summary>
    public static PropertyEditor CreateEditor(Action<ImageSource?> set) => new(EditorKind.Text, null, text =>
    {
        text = text.Trim();
        try
        {
            if (text.Length == 0)
                set(null);
            else if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                     || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                set(ImageSource.FromUri(new Uri(text)));
            else
                set(ImageSource.FromFile(text));
            return true;
        }
        catch
        {
            return false;
        }
    });
}
