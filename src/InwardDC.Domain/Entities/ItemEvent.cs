using InwardDC.Domain.Enums;

namespace InwardDC.Domain.Entities;

/// <summary>
/// Item lifecycle event recorded for the item history / timeline screen. Queries can
/// answer "everything that happened to item X" or "everything that happened to
/// serial number S".
/// </summary>
public class ItemEvent : EntityBase
{
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }
    public string SerialNo { get; set; } = string.Empty;
    public ItemEventType EventType { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Notes { get; set; } = string.Empty;
    public Guid? EventedBy { get; set; }
    public DateTime EventedOn { get; set; } = DateTime.UtcNow;
}
