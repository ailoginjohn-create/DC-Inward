using InwardDC.Domain.Enums;

namespace InwardDC.Domain.Entities;

/// <summary>
/// Physical unit / serial number record. Created when an inward entry brings the
/// unit in. The status and DcId fields let the application answer "where is this
/// serial right now?" and drive the item history timeline.
/// </summary>
public class SerialNumber : EntityBase
{
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public string SerialNo { get; set; } = string.Empty;
    public SerialStatus Status { get; set; } = SerialStatus.InStock;
    public Guid? InwardEntryId { get; set; }
    public InwardEntry? InwardEntry { get; set; }
    public Guid? InwardItemId { get; set; }
    public InwardItem? InwardItem { get; set; }
    public Guid? DispatchChallanId { get; set; }
    public DispatchChallan? DispatchChallan { get; set; }
    public Guid? DispatchItemId { get; set; }
    public DispatchItem? DispatchItem { get; set; }
    public DateTime? DispatchedOn { get; set; }
    public string Notes { get; set; } = string.Empty;
}
