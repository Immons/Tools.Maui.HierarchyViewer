using System.Reflection;

namespace Immons.Tools.Maui.Inspector.Shared;

/// <summary>
/// Version of the inspector package the app was built against, shown in the panel header so it is
/// obvious which build is running (and whether it is the current one).
/// </summary>
internal static class PackageVersion
{
    static string? _value;

    public static string Current => _value ??= Read();

    static string Read()
    {
        try
        {
            var assembly = typeof(MauiInspector).Assembly;
            // InformationalVersion carries the NuGet version; SourceLink appends "+<commit>".
            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(informational))
            {
                var plus = informational.IndexOf('+');
                return plus > 0 ? informational[..plus] : informational;
            }

            return assembly.GetName().Version?.ToString(3) ?? "";
        }
        catch
        {
            return "";
        }
    }
}
