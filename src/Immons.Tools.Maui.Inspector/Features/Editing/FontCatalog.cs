using System.Collections;
using System.Reflection;

namespace Immons.Tools.Maui.Inspector.Features.Editing;

/// <summary>
/// Font aliases registered with ConfigureFonts. IFontRegistrar exposes no enumeration, so the
/// registry dictionary is read by reflection; an empty list simply means no suggestions.
/// </summary>
internal static class FontCatalog
{
    static readonly string[] FontFileExtensions = [".ttf", ".otf", ".woff", ".woff2"];

    static bool HasFontFileExtension(string value) =>
        FontFileExtensions.Any(e => value.EndsWith(e, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<string> RegisteredAliases()
    {
        try
        {
            var services = MauiInspector.ActiveInspector?.MauiContext?.Services;
            var registrar = services?.GetService(typeof(IFontRegistrar));
            if (registrar == null)
                return [];

            var aliases = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in registrar.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.GetValue(registrar) is not IDictionary map)
                    continue;
                foreach (var key in map.Keys)
                {
                    // The registry also keys raw file names — only aliases are settable values.
                    if (key is string alias && alias.Length > 0 && !HasFontFileExtension(alias))
                        aliases.Add(alias);
                }
            }
            return aliases.ToList();
        }
        catch
        {
            return [];
        }
    }
}
