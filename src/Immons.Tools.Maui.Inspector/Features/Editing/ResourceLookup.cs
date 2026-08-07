using System.Reflection;

namespace Immons.Tools.Maui.Inspector.Features.Editing;

/// <summary>
/// Deep resource lookup: walks the element's parent chain (every <see cref="Element"/>, not just
/// visual ones) and the application resources, descending into merged dictionaries — including
/// the private instance that holds the content of dictionaries loaded via <c>Source=</c>, which
/// the public indexer does not see.
/// </summary>
internal static class ResourceLookup
{
    static readonly FieldInfo? MergedInstanceField =
        typeof(ResourceDictionary).GetField("_mergedInstance", BindingFlags.NonPublic | BindingFlags.Instance);

    public static bool TryFind(object? context, string key, out object? value)
    {
        var seen = new HashSet<ResourceDictionary>();

        for (var current = context as Element; current != null; current = current.Parent)
        {
            if (ResourcesOf(current) is { } rd && TryFindIn(rd, key, seen, out value))
                return true;
        }

        if (Application.Current?.Resources is { } appResources && TryFindIn(appResources, key, seen, out value))
            return true;

        value = null;
        return false;
    }

    /// <summary>Keys of resources whose value fits the property type (for editor suggestions).</summary>
    public static IReadOnlyList<string> CompatibleKeys(object? context, Type propertyType)
    {
        var seen = new HashSet<ResourceDictionary>();
        var keys = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        void Collect(ResourceDictionary? rd)
        {
            if (rd == null || !seen.Add(rd))
                return;

            try
            {
                foreach (var pair in rd)
                {
                    if (Fits(pair.Value, propertyType))
                        keys.Add(pair.Key);
                }
            }
            catch
            {
                // enumeration can throw on exotic dictionaries — skip it
            }

            foreach (var merged in rd.MergedDictionaries)
                Collect(merged);

            try
            {
                Collect(MergedInstanceField?.GetValue(rd) as ResourceDictionary);
            }
            catch
            {
                // see TryFind
            }
        }

        for (var current = context as Element; current != null; current = current.Parent)
            Collect(ResourcesOf(current));
        Collect(Application.Current?.Resources);

        return keys.ToList();
    }

    /// <summary>Key under which this exact instance lives in a reachable dictionary; null when none.</summary>
    public static string? KeyOf(object? context, object value)
    {
        var seen = new HashSet<ResourceDictionary>();
        string? found = null;

        void Search(ResourceDictionary? rd)
        {
            if (rd == null || found != null || !seen.Add(rd))
                return;
            try
            {
                foreach (var pair in rd)
                {
                    if (ReferenceEquals(pair.Value, value))
                    {
                        found = pair.Key;
                        return;
                    }
                }
            }
            catch
            {
                // see TryFind
            }
            foreach (var merged in rd.MergedDictionaries)
                Search(merged);
            try
            {
                Search(MergedInstanceField?.GetValue(rd) as ResourceDictionary);
            }
            catch
            {
                // see TryFind
            }
        }

        for (var current = context as Element; current != null; current = current.Parent)
            Search(ResourcesOf(current));
        Search(Application.Current?.Resources);
        return found;
    }

    /// <summary>
    /// Key of the resource a resolved value came from: reference identity first; boxed
    /// structs (Thickness, CornerRadius…) can lose identity through conversion, so a
    /// value-equal match of the same type is accepted when it is unambiguous.
    /// </summary>
    public static string? KeyOfResolved(object? context, object value)
    {
        if (KeyOf(context, value) is { } exact)
            return exact;

        string? found = null;
        var ambiguous = false;
        foreach (var (key, candidate) in AllPairs(context))
        {
            if (candidate == null || candidate.GetType() != value.GetType() || !Equals(candidate, value))
                continue;
            if (found != null && found != key)
            {
                ambiguous = true;
                break;
            }
            found = key;
        }
        return ambiguous ? null : found;
    }

    static IEnumerable<(string Key, object? Value)> AllPairs(object? context)
    {
        var seen = new HashSet<ResourceDictionary>();
        var results = new List<(string, object?)>();

        void Collect(ResourceDictionary? rd)
        {
            if (rd == null || !seen.Add(rd))
                return;
            try
            {
                foreach (var pair in rd)
                    results.Add((pair.Key, pair.Value));
            }
            catch
            {
                // see TryFind
            }
            foreach (var merged in rd.MergedDictionaries)
                Collect(merged);
            try
            {
                Collect(MergedInstanceField?.GetValue(rd) as ResourceDictionary);
            }
            catch
            {
                // see TryFind
            }
        }

        for (var current = context as Element; current != null; current = current.Parent)
            Collect(ResourcesOf(current));
        Collect(Application.Current?.Resources);
        return results;
    }

    static bool Fits(object? value, Type propertyType)
    {
        if (value == null || value is Style || value is ResourceDictionary)
            return false;
        if (propertyType.IsInstanceOfType(value))
            return true;
        if (propertyType == typeof(Color) && value is SolidColorBrush)
            return true;
        if (typeof(Brush).IsAssignableFrom(propertyType) && value is Color)
            return true;
        // numeric conversions the editors already perform (a string is never a number here)
        return propertyType.IsPrimitive && value is not string && value is IConvertible;
    }

    /// <summary>Resources of any element that has them — IResourcesProvider is internal, so read the property.</summary>
    static ResourceDictionary? ResourcesOf(Element element) => element switch
    {
        VisualElement ve => ve.Resources,
        Application app => app.Resources,
        _ => element.GetType()
            .GetProperty("Resources", BindingFlags.Public | BindingFlags.Instance)?
            .GetValue(element) as ResourceDictionary,
    };

    static bool TryFindIn(ResourceDictionary rd, string key, HashSet<ResourceDictionary> seen, out object? value)
    {
        value = null;
        if (!seen.Add(rd))
            return false;

        try
        {
            if (rd.TryGetValue(key, out value))
                return true;
        }
        catch
        {
            // a dictionary can throw while resolving a DynamicResource — keep searching
        }

        foreach (var merged in rd.MergedDictionaries)
        {
            if (TryFindIn(merged, key, seen, out value))
                return true;
        }

        try
        {
            if (MergedInstanceField?.GetValue(rd) is ResourceDictionary inner && TryFindIn(inner, key, seen, out value))
                return true;
        }
        catch
        {
            // internal layout changed — Source-loaded dictionaries just won't be searched
        }

        return false;
    }
}
