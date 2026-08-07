namespace InwardDC.Domain.Entities;

/// <summary>
/// Key/value configuration store. All configurable values (company info, numbering
/// prefixes, paths, database provider overrides) live here instead of being
/// hardcoded, so the application can be configured at runtime.
/// </summary>
public class Setting : EntityBase
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public string DataType { get; set; } = "string";
}
