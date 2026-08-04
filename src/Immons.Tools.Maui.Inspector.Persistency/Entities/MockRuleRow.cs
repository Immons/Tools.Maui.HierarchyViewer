using SQLite;

namespace Immons.Tools.Maui.Inspector.Persistency.Entities;

/// <summary>
/// One mock rule as a row. Bodies live in their own columns so a rule can be rewritten without
/// touching anything else — the point of this backend.
/// </summary>
[Table("mock_rules")]
internal sealed class MockRuleRow
{
    [PrimaryKey]
    public int Id { get; set; }

    public bool Enabled { get; set; }

    public string Method { get; set; } = "*";

    [Indexed]
    public string UrlPattern { get; set; } = "";

    public string? Name { get; set; }

    public int DelayMs { get; set; }

    public string FailMode { get; set; } = "";

    public bool ShortCircuit { get; set; }

    public int? Status { get; set; }

    public string? RequestBody { get; set; }

    public string? ResponseBody { get; set; }

    /// <summary>Newline-separated scenario names — a rule belongs to few, and they are never queried alone.</summary>
    public string? Scenarios { get; set; }
}
