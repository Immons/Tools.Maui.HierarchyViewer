using System.Reflection;
using System.Text.Json.Nodes;
using Immons.Tools.Maui.Inspector.Features.VisualTree;

namespace Immons.Tools.Maui.Inspector.Features.Properties;

/// <summary>
/// Unique AutomationIds for data-templated items. Every instance of a DataTemplate shares
/// one XAML line, so a literal AutomationId can never tell the rows apart — but the items'
/// data can: AutomationId is a bindable property, and "{Binding Id, StringFormat='card-{0}'}"
/// yields a different id per row. This inspects the live instances' BindingContexts to find
/// properties whose values actually are unique, then binds them all and records the markup.
/// </summary>
internal sealed class AutomationIdBinder(
    IElementRegistry elements,
    IActiveInspectorProvider inspectors,
    IXamlChangeLog xamlChanges)
{
    public string Candidates(int id)
    {
        if (elements.Find(id) is not { } element)
            return Error("element not found");

        var instances = Instances(element);
        var contexts = instances.Select(i => i.BindingContext).ToList();
        var sample = contexts.FirstOrDefault(c => c != null);
        if (sample == null)
            return Error("the element has no BindingContext — is it a data-templated item?");

        var candidates = new JsonArray();
        foreach (var property in sample.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0 || !IsSimple(property.PropertyType))
                continue;

            var values = contexts.Select(context =>
            {
                try
                {
                    return context == null ? null : property.GetValue(context)?.ToString();
                }
                catch
                {
                    return null;
                }
            }).ToList();

            var filled = values.Where(v => !string.IsNullOrEmpty(v)).ToList();
            var unique = filled.Count == values.Count && filled.Distinct().Count() == filled.Count;
            var preview = new JsonArray();
            foreach (var value in values.Take(3))
                preview.Add(value ?? "(null)");

            candidates.Add(new JsonObject
            {
                ["name"] = property.Name,
                ["unique"] = unique,
                ["preview"] = preview,
            });
        }

        var sorted = new JsonArray();
        foreach (var node in candidates
                     .OrderByDescending(c => c!["unique"]!.GetValue<bool>())
                     .ThenBy(c => c!["name"]!.GetValue<string>(), StringComparer.OrdinalIgnoreCase)
                     .ToList())
        {
            candidates.Remove(node);
            sorted.Add(node);
        }

        return new JsonObject
        {
            ["count"] = instances.Count,
            ["type"] = element.GetType().Name,
            ["candidates"] = sorted,
        }.ToJsonString();
    }

    public (bool Ok, string? Error) Bind(int id, string path, string format)
    {
        return elements.Find(id) is { } element
            ? BindCore(element, path, format)
            : (false, "element not found");
    }

    /// <summary>
    /// One-tap variant for the on-device panel: picks the best unique property by itself —
    /// "Id"-ish names first — and prefixes with the element's type name.
    /// </summary>
    public (bool Ok, string? Error) BindBest(VisualElement element)
    {
        var contexts = Instances(element).Select(i => i.BindingContext).ToList();
        var sample = contexts.FirstOrDefault(c => c != null);
        if (sample == null)
            return (false, "no BindingContext");

        string? best = null;
        var bestRank = int.MaxValue;
        foreach (var property in sample.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0 || !IsSimple(property.PropertyType))
                continue;
            var values = contexts.Select(c =>
            {
                try
                {
                    return c == null ? null : property.GetValue(c)?.ToString();
                }
                catch
                {
                    return null;
                }
            }).ToList();
            var filled = values.Where(v => !string.IsNullOrEmpty(v)).ToList();
            if (filled.Count != values.Count || filled.Distinct().Count() != filled.Count)
                continue;

            var rank = property.Name == "Id" ? 0
                : property.Name.EndsWith("Id", StringComparison.Ordinal) ? 1
                : 2;
            if (rank < bestRank)
            {
                bestRank = rank;
                best = property.Name;
            }
        }
        if (best == null)
            return (false, "no property with unique values across the instances");

        return BindCore(element, best, element.GetType().Name.ToLowerInvariant() + "-{0}");
    }

    (bool Ok, string? Error) BindCore(VisualElement element, string path, string format)
    {
        if (string.IsNullOrWhiteSpace(path))
            return (false, "pick a property");

        foreach (var instance in Instances(element))
        {
            var binding = new Binding(path);
            if (!string.IsNullOrEmpty(format))
                binding.StringFormat = format;
            // SetBinding on the bindable property side-steps the CLR setter's
            // "may only be set one time" guard — by design here.
            instance.SetBinding(Element.AutomationIdProperty, binding);
        }

        var markup = string.IsNullOrEmpty(format)
            ? $"{{Binding {path}}}"
            : $"{{Binding {path}, StringFormat='{format}'}}";
        xamlChanges.Record(element, "AutomationId", markup);
        inspectors.Current?.RemoteAfterEdit();
        return (true, null);
    }

    /// <summary>Every live element sharing the XAML source line — the template's instances.</summary>
    List<VisualElement> Instances(VisualElement element)
    {
        var source = XamlSource.Describe(element);
        if (source == null)
            return [element];

        var result = new List<VisualElement>();

        void Walk(VisualElement current)
        {
            if (XamlSource.Describe(current) == source)
                result.Add(current);
            foreach (var child in VisualTreeWalker.GetVisualChildren(current))
                Walk(child);
        }

        foreach (var root in inspectors.Current?.Roots ?? [])
            Walk(root);
        return result.Count > 0 ? result : [element];
    }

    /// <summary>
    /// True when the element lives inside an items host (CollectionView, CarouselView,
    /// ListView, or a layout with BindableLayout.ItemsSource) — the only place where a
    /// per-item AutomationId makes sense. Walks up, so a Label deep inside a tile counts.
    /// </summary>
    public static bool IsTemplatedItem(VisualElement element)
    {
        for (var current = element.Parent; current != null; current = current.Parent)
        {
            if (current is ItemsView or ListView)
                return true;
            if (current is Layout layout && layout.IsSet(BindableLayout.ItemsSourceProperty))
                return true;
            if (current is Page)
                break;
        }
        return false;
    }

    static bool IsSimple(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(string) || type.IsPrimitive || type.IsEnum
               || type == typeof(Guid) || type == typeof(decimal);
    }

    static string Error(string message) => new JsonObject { ["error"] = message }.ToJsonString();
}
