namespace Immons.Tools.Maui.Inspector.Shared;

/// <summary>Element descriptions shared by tree labels, dumps, edit history and the web client.</summary>
internal static class ElementInfo
{
    public static Thickness? GetPadding(VisualElement element) => element switch
    {
        Layout l => l.Padding,
        ContentPage p => p.Padding,
        Border b => b.Padding,
        Button b => b.Padding,
        ImageButton b => b.Padding,
        Label l => l.Padding,
        _ => (element as IPadding)?.Padding,
    };

    /// <summary>Identifier tag by the "@x:Name #AutomationId" convention; null when the element has neither.</summary>
    public static string? IdTag(VisualElement element)
    {
        string? tag = null;
        if (!string.IsNullOrEmpty(element.StyleId))
            tag = $"@{element.StyleId}";
        if (!string.IsNullOrEmpty(element.AutomationId) && element.AutomationId != element.StyleId)
            tag = tag == null ? $"#{element.AutomationId}" : $"{tag} #{element.AutomationId}";
        return tag;
    }

    /// <summary>"TypeName @x:Name #AutomationId" (ids only when present).</summary>
    public static string ShortLabel(VisualElement element)
    {
        var name = element.GetType().Name;
        return IdTag(element) is { } tag ? $"{name} {tag}" : name;
    }
}
