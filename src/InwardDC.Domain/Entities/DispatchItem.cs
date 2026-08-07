namespace InwardDC.Domain.Entities;

/// <summary>
/// Line item of a dispatch challan. Optionally linked back to the source inward line
/// (InwardItemId) so available stock/quantities can be computed.
/// </summary>
public class DispatchItem : EntityBase
{
    public Guid DispatchChallanId { get; set; }
    public DispatchChallan? DispatchChallan { get; set; }
    public Guid? SourceInwardItemId { get; set; }
    public InwardItem? SourceInwardItem { get; set; }
    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }

    public string ItemName { get; set; } = string.Empty;
    public string ItemMake { get; set; } = string.Empty;
    public string ItemModel { get; set; } = string.Empty;
    public string HsnCode { get; set; } = string.Empty;
    public string Unit { get; set; } = "Nos";
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public string Remarks { get; set; } = string.Empty;

    public ICollection<SerialNumber> Serials { get; set; } = new List<SerialNumber>();
}
