namespace InwardDC.Domain.Entities;

/// <summary>Line item of an inward entry.</summary>
public class InwardItem : EntityBase
{
    public Guid InwardEntryId { get; set; }
    public InwardEntry? InwardEntry { get; set; }
    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }

    /// <summary>Snapshot fields keep the entry meaningful even if the master changes.</summary>
    public string ItemName { get; set; } = string.Empty;
    public string ItemMake { get; set; } = string.Empty;
    public string ItemModel { get; set; } = string.Empty;
    public string HsnCode { get; set; } = string.Empty;
    public string Unit { get; set; } = "Nos";
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal DispatchedQuantity { get; set; }
    public string Remarks { get; set; } = string.Empty;

    public ICollection<SerialNumber> Serials { get; set; } = new List<SerialNumber>();
}
