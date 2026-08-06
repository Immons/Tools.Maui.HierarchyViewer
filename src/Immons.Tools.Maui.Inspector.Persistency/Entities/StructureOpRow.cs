using SQLite;

namespace Immons.Tools.Maui.Inspector.Persistency.Entities;

/// <summary>A persisted structural edit (add/remove element), serialized StructureOp JSON.</summary>
[Table("structure_ops")]
internal sealed class StructureOpRow
{
    [PrimaryKey]
    public string Id { get; set; } = "";

    public string Json { get; set; } = "";
}
