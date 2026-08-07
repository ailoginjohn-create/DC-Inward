namespace InwardDC.Domain.Entities;

/// <summary>Item category (e.g., Biomedical Equipment, Consumables, Spare Parts).</summary>
public class ItemCategory : EntityBase
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
