using InwardDC.Domain.Enums;

namespace InwardDC.Domain.Entities;

/// <summary>
/// Dispatch Challan (DC) header. A DC dispatches goods (usually received through an
/// inward entry) to a customer.
/// </summary>
public class DispatchChallan : EntityBase
{
    public string DcNo { get; set; } = string.Empty;
    public DateTime DcDate { get; set; } = DateTime.Today;
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? PurposeId { get; set; }
    public Purpose? Purpose { get; set; }
    public Guid? SourceInwardEntryId { get; set; }
    public InwardEntry? SourceInwardEntry { get; set; }
    public string ReferenceChallanNo { get; set; } = string.Empty;
    public string TransportDetails { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public DispatchStatus Status { get; set; } = DispatchStatus.Draft;

    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<DispatchItem> Items { get; set; } = new List<DispatchItem>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
