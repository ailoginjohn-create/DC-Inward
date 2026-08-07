namespace InwardDC.Domain.Entities;

/// <summary>
/// Yearly auto-number counter used to generate human-friendly document numbers such
/// as "INW/2026/0001" and "DC/2026/0001". One row per entity type per year.
/// </summary>
public class SequenceCounter : EntityBase
{
    public string EntityName { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int Year { get; set; }
    public int LastNumber { get; set; }
}
