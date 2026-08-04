using System.Reflection;

namespace Immons.Tools.Maui.Inspector.Features.Properties;

/// <summary>Discovers the styles reachable from an element.</summary>
internal static class StyleCatalog
{
    // Dictionaries loaded via Source= keep their content in a private merged instance that the
    // public enumerator skips — reach it by reflection.
    static readonly FieldInfo? MergedInstanceField =
        typeof(ResourceDictionary).GetField("_mergedInstance", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>All styles reachable from the element (its resources, ancestors, application),
    /// including merged and Source-loaded dictionaries, whose TargetType matches the element.</summary>
    public static List<(string Key, Style Style)> CollectStyles(VisualElement el)
    {
        var result = new List<(string, Style)>();
        var seenStyles = new HashSet<Style>();
        var seenDicts = new HashSet<ResourceDictionary>();

        void Scan(ResourceDictionary? rd)
        {
            if (rd == null || !seenDicts.Add(rd))
                return;
            foreach (var kv in rd)
            {
                if (kv.Value is Style style
                    && style.TargetType?.IsAssignableFrom(el.GetType()) == true
                    && seenStyles.Add(style))
                    result.Add((kv.Key, style));
            }
            foreach (var merged in rd.MergedDictionaries)
                Scan(merged);

            try
            {
                if (MergedInstanceField?.GetValue(rd) is ResourceDictionary inner)
                    Scan(inner);
            }
            catch
            {
                // internal layout changed — Source-loaded dictionaries just won't be listed
            }
        }

        for (Element? current = el; current != null; current = current.Parent)
        {
            if (current is VisualElement { Resources: { } rd })
                Scan(rd);
        }
        Scan(Application.Current?.Resources);

        result.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
        return result;
    }
}
