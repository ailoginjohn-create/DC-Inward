namespace InwardDC.Domain.Entities;

/// <summary>
/// Item master (catalog of products/equipment). Serial number tracking is enabled
/// when <see cref="IsSerialTracked"/> is true — each physical unit then gets a
/// dedicated serial record enabling full item history / timeline.
/// </summary>
public class Item : EntityBase
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public ItemCategory? Category { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Unit { get; set; } = "Nos";
    public string HsnCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSerialTracked { get; set; }
    public bool IsActive { get; set; } = true;
}
