using System.Reflection;
using System.Text.RegularExpressions;

namespace Immons.Tools.Maui.Inspector.Features.Editing;

/// <summary>
/// Resolves custom markup extensions typed into an editor, e.g. "{extensions:Translate Key}".
/// The extension type is located by name among the loaded assemblies and its single positional
/// argument is assigned to the ContentProperty, mirroring what the XAML parser does.
/// </summary>
internal static class MarkupExtensionResolver
{
    static readonly Regex Expression = new(
        @"^\{\s*(?:(?<ns>[\w.]+):)?(?<name>[\w.]+)(?:\s+(?<arg>[^}]*))?\}$", RegexOptions.Compiled);

    /// <summary>Any "{…}" text that is not one of the built-ins handled elsewhere.</summary>
    public static bool IsCustomMarkup(string text)
    {
        var match = Expression.Match(text.Trim());
        if (!match.Success)
            return false;
        var name = match.Groups["name"].Value;
        return name is not ("Binding" or "StaticResource" or "DynamicResource" or "OnPlatform" or "OnIdiom");
    }

    /// <summary>Runs the extension and returns its value; null when it cannot be resolved.</summary>
    public static object? TryResolve(string text, object? target)
    {
        var match = Expression.Match(text.Trim());
        if (!match.Success)
            return null;

        var name = match.Groups["name"].Value;
        var argument = match.Groups["arg"].Success ? match.Groups["arg"].Value.Trim().Trim('\'') : null;

        if (FindExtensionType(name) is not { } type)
            return null;

        try
        {
            if (Activator.CreateInstance(type) is not { } extension)
                return null;

            if (argument is { Length: > 0 } && ContentPropertyOf(type) is { } contentProperty)
                contentProperty.SetValue(extension, Convert.ChangeType(argument, contentProperty.PropertyType));

            var provideValue = type.GetMethod("ProvideValue", [typeof(IServiceProvider)]);
            return provideValue?.Invoke(extension, [new MarkupServiceProvider(target)]);
        }
        catch
        {
            return null; // extension needs more context than we can fake — caller falls back
        }
    }

    static Type? FindExtensionType(string name)
    {
        var candidates = new[] { name, name + "Extension" };
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch
            {
                continue; // dynamic or partially loaded assembly
            }

            foreach (var type in types)
            {
                if (!candidates.Contains(type.Name, StringComparer.Ordinal))
                    continue;
                if (type.GetInterfaces().Any(i => i.Name.StartsWith("IMarkupExtension", StringComparison.Ordinal)))
                    return type;
            }
        }
        return null;
    }

    /// <summary>The property that receives the positional argument ([ContentProperty] or the only string one).</summary>
    static PropertyInfo? ContentPropertyOf(Type type)
    {
        var attribute = type.GetCustomAttributes()
            .FirstOrDefault(a => a.GetType().Name == "ContentPropertyAttribute");
        if (attribute?.GetType().GetProperty("Name")?.GetValue(attribute) is string contentName
            && type.GetProperty(contentName) is { CanWrite: true } named)
            return named;

        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p is { CanWrite: true } && p.PropertyType == typeof(string));
    }

    /// <summary>Minimal service provider — enough for extensions that only need the target object.</summary>
    sealed class MarkupServiceProvider(object? target) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType.IsInstanceOfType(target) ? target : null;
    }
}
