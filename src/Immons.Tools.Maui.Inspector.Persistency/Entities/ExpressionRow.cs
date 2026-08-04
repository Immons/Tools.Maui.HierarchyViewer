using SQLite;

namespace Immons.Tools.Maui.Inspector.Persistency.Entities;

/// <summary>An applied editor expression keyed by the element's XAML identity hash.</summary>
[Table("expressions")]
internal sealed class ExpressionRow
{
    [PrimaryKey]
    public string Key { get; set; } = "";

    public string? Expression { get; set; }
}
