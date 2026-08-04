using SQLite;

namespace Immons.Tools.Maui.Inspector.Persistency.Entities;

/// <summary>Small single values: active scenario, scenario registry, breakpoint settings.</summary>
[Table("settings")]
internal sealed class SettingRow
{
    [PrimaryKey]
    public string Key { get; set; } = "";

    public string? Value { get; set; }
}
