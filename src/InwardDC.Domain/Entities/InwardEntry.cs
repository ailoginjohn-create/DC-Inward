using InwardDC.Domain.Enums;

namespace InwardDC.Domain.Entities;

/// <summary>
/// Inward (goods receipt) header. Records material/equipment received either from
/// a customer (return/service) or a vendor (purchase).
/// </summary>
public class InwardEntry : EntityBase
{
    public string InwardNo { get; set; } = string.Empty;
    public DateTime InwardDate { get; set; } = DateTime.Today;
    public InwardType InwardType { get; set; } = InwardType.CustomerReturn;
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public Guid? PurposeId { get; set; }
    public Purpose? Purpose { get; set; }
    public string ReferenceInvoiceNo { get; set; } = string.Empty;
    public DateTime? ReferenceInvoiceDate { get; set; }
    public string ChallanNo { get; set; } = string.Empty;
    public string TransportDetails { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public InwardStatus Status { get; set; } = InwardStatus.Draft;

    /// <summary>Denormalized totals to keep reporting fast.</summary>
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<InwardItem> Items { get; set; } = new List<InwardItem>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
