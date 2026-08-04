namespace Immons.Tools.Maui.Inspector.Features.Properties.Web;

/// <summary>
/// Logical page name behind per-device page variants. Apps that pick a whole page per form factor
/// ("PlanVisit_iPhone_Page", "PlanVisit_Android_Tablet_Page", "PlanVisitPage") all map to
/// "PlanVisit", so a remote edit can be confined to the counterpart page instead of matching a
/// same-named element on an unrelated screen.
/// </summary>
internal static class PageIdentity
{
    static readonly string[] VariantTokens =
    [
        "iPhone", "iPad", "Phone", "Tablet", "Desktop", "Watch", "TV",
        "Android", "iOS", "Windows", "WinUI", "Mac", "MacCatalyst",
        "Landscape", "Portrait", "Wide", "Compact", "Small", "Large",
    ];

    /// <summary>Logical name of the page an element belongs to; empty when it has no page.</summary>
    public static string Of(Element element)
    {
        for (Element? current = element; current != null; current = current.Parent)
        {
            if (current is Page page)
                return Normalize(page.GetType().Name);
        }
        return "";
    }

    static string Normalize(string typeName)
    {
        var parts = typeName.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !VariantTokens.Contains(p, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (parts.Count == 0)
            return typeName;

        var name = string.Concat(parts);
        return name.EndsWith("Page", StringComparison.Ordinal) && name.Length > 4
            ? name[..^4]
            : name;
    }
}
