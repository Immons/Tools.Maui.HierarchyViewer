using System.Collections.Concurrent;
using System.Reflection;

namespace Immons.Tools.Maui.Inspector.Web.Hosting;

/// <summary>Loads the web client's files from embedded resources (Web/Assets/**), cached.</summary>
internal static class WebAssets
{
    const string Prefix = "Immons.Tools.Maui.Inspector.Web.Assets.";

    static readonly ConcurrentDictionary<string, string?> Cache = new();

    /// <summary>Resource content by logical name ("index.html", "app.css", "js.tree.js"); null when absent.</summary>
    public static string? Read(string name) => Cache.GetOrAdd(name, static key =>
    {
        using var stream = typeof(WebAssets).Assembly.GetManifestResourceStream(Prefix + key);
        if (stream == null)
            return null;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });
}
