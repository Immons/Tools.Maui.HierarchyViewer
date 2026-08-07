using System.Reflection;

namespace Immons.Tools.Maui.Inspector.Features.Structure;

/// <summary>
/// "Extract style": turns an element's local property values into a keyed
/// <see cref="Style"/> in the page's resources and re-points the element at it —
/// live, with undo, and mirrored into the XAML sources.
/// </summary>
internal sealed class StyleExtractor(IXamlChangeLog xamlChanges)
{
    /// <summary>Content-ish properties: legal in a Style, but rarely what a style should carry.</summary>
    static readonly HashSet<string> NotPreselected =
    [
        "Text", "Content", "ItemsSource", "Command", "CommandParameter", "Source", "IconSource",
        "Placeholder", "Title",
    ];

    internal sealed record Candidate(string Name, string Value, bool Preselected, BindableProperty Property, object? RawValue);

    internal sealed record Extraction(Style Style, string Key, List<(BindableProperty Property, object? OldValue)> Extracted);

    /// <summary>Local, style-able values of the element (differing from the type's defaults).</summary>
    public static List<Candidate> Candidates(View element)
    {
        var result = new List<Candidate>();
        View baseline;
        try
        {
            baseline = (View)Activator.CreateInstance(element.GetType())!;
        }
        catch
        {
            return result;
        }

        foreach (var property in element.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.SetMethod is not { IsPublic: true } || property.GetIndexParameters().Length > 0)
                continue;
            if (ReflectionLookup.FindBindableProperty(element.GetType(), property.Name) is not { } bindable)
                continue;
            if (BindingDescriptor.Describe(element, property.Name) != null)
                continue; // bound values belong to the binding, not a style

            object? value;
            object? defaultValue;
            try
            {
                value = property.GetValue(element);
                defaultValue = property.GetValue(baseline);
            }
            catch
            {
                continue;
            }
            if (Equals(value, defaultValue) || value == null)
                continue;
            if (ElementCloner.XamlAttributeValue(value) is not { } text)
                continue;

            result.Add(new Candidate(
                property.Name, text,
                Preselected: !NotPreselected.Contains(property.Name),
                bindable, value));
        }
        return result.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Performs the live part: builds the style, clears the extracted local values and assigns
    /// the style to the element. Returns what is needed for undo.
    /// </summary>
    public static (Extraction? Result, string? Error) Extract(View element, Page page, string key, IReadOnlyCollection<string> propertyNames)
    {
        if (string.IsNullOrWhiteSpace(key))
            return (null, "the style needs a key");
        if (page.Resources.ContainsKey(key))
            return (null, $"resource key \"{key}\" already exists on the page");

        var candidates = Candidates(element).Where(c => propertyNames.Contains(c.Name)).ToList();
        if (candidates.Count == 0)
            return (null, "no properties selected");

        var style = new Style(element.GetType());
        var extracted = new List<(BindableProperty, object?)>();
        foreach (var candidate in candidates)
        {
            style.Setters.Add(new Setter { Property = candidate.Property, Value = candidate.RawValue });
            extracted.Add((candidate.Property, candidate.RawValue));
        }

        page.Resources[key] = style;
        foreach (var (property, _) in extracted)
            element.ClearValue(property);
        element.Style = style;

        return (new Extraction(style, key, extracted), null);
    }

    /// <summary>The XAML side: the style block into page resources, the element re-attributed.</summary>
    public void WriteBack(View element, Extraction extraction, StructureOp op)
    {
        xamlChanges.RecordStyleResource(op);

        // The element loses the extracted attributes and gains Style="{StaticResource key}" —
        // ordinary attribute changes, which also cover inspector-added elements via their op.
        foreach (var (property, _) in extraction.Extracted)
            xamlChanges.Record(element, property.PropertyName, XamlChangeLog.RemoveMarker);
        xamlChanges.Record(element, "Style", $"{{StaticResource {extraction.Key}}}");
    }

    /// <summary>Reverses <see cref="WriteBack"/> for undo.</summary>
    public void UndoWriteBack(View element, Extraction extraction, StructureOp op)
    {
        xamlChanges.CancelStyleResource(op);
        foreach (var candidate in extraction.Extracted)
        {
            if (candidate.OldValue != null && ElementCloner.XamlAttributeValue(candidate.OldValue) is { } text)
                xamlChanges.Record(element, candidate.Property.PropertyName, text);
        }
        xamlChanges.Record(element, "Style", XamlChangeLog.RemoveMarker);
    }

    /// <summary>Builds the persisted/write-back op: setters + xmlns for a custom TargetType.</summary>
    public static StructureOp BuildOp(View element, Page page, string key, Extraction extraction)
    {
        var type = element.GetType();
        var xmlns = new Dictionary<string, string>();
        string targetTypeText;
        if (type.Namespace?.StartsWith("Microsoft.Maui.Controls", StringComparison.Ordinal) == true)
        {
            targetTypeText = type.Name;
        }
        else
        {
            xmlns["p1"] = $"clr-namespace:{type.Namespace};assembly={type.Assembly.GetName().Name}";
            targetTypeText = $"p1:{type.Name}";
        }

        var setters = new Dictionary<string, string>();
        foreach (var (property, value) in extraction.Extracted)
        {
            if (value != null && ElementCloner.XamlAttributeValue(value) is { } text)
                setters[property.PropertyName] = text;
        }

        var lines = new List<string> { $"<Style x:Key=\"{key}\" TargetType=\"{targetTypeText}\">" };
        foreach (var (name, value) in setters)
            lines.Add($"    <Setter Property=\"{name}\" Value=\"{Escape(value)}\" />");
        lines.Add("</Style>");

        return new StructureOp(
            Guid.NewGuid().ToString("N"),
            StructureOp.KindStyle,
            ParentIdentity: XamlSource.Describe(page),
            page.GetType().Name,
            type.FullName!,
            type.Assembly.GetName().Name ?? "",
            type.Name,
            XamlSource.Describe(element),
            setters,
            Order: DateTime.UtcNow.Ticks,
            SnippetXml: string.Join("\n", lines),
            SnippetXmlns: xmlns.Count > 0 ? xmlns : null,
            StyleKey: key);
    }

    static string Escape(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace("\"", "&quot;");
}
