using System.Reflection;

namespace Immons.Tools.Maui.Inspector.Features.Properties;

/// <summary>The well-known Keyboard singletons (Keyboard.Default, .Email, …) by name.</summary>
internal static class KeyboardCatalog
{
    static readonly Dictionary<string, Keyboard> ByName =
        typeof(Keyboard).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(Keyboard))
            .ToDictionary(p => p.Name, p => (Keyboard)p.GetValue(null)!);

    public static string NameOf(Keyboard? keyboard)
    {
        if (keyboard == null)
            return "Default";
        foreach (var (name, kb) in ByName)
        {
            if (ReferenceEquals(kb, keyboard))
                return name;
        }
        return keyboard.GetType().Name;
    }

    public static PropertyEditor CreateEditor(InputView input) => new(
        EditorKind.Enum,
        ByName.Keys.OrderBy(k => k).ToList(),
        name =>
        {
            if (!ByName.TryGetValue(name, out var keyboard))
                return false;
            input.Keyboard = keyboard;
            return true;
        })
    {
        XamlTarget = input,
        XamlAttribute = "Keyboard",
    };
}
