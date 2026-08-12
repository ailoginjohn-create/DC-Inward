using InwardDC.Domain.Enums;

namespace InwardDC.Application.DTOs;

public class InwardItemDto
{
    public Guid Id { get; set; }
    public Guid? ItemId { get; set; }
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
    public List<string> Serials { get; set; } = new();
}

public class InwardDto
{
    public Guid Id { get; set; }
    public string InwardNo { get; set; } = string.Empty;
    public DateTime InwardDate { get; set; }
    public InwardType InwardType { get; set; }
    public string InwardTypeName => InwardType.ToString();
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid? VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public Guid? PurposeId { get; set; }
    public string PurposeName { get; set; } = string.Empty;
    public string ReferenceInvoiceNo { get; set; } = string.Empty;
    public DateTime? ReferenceInvoiceDate { get; set; }
    public string ChallanNo { get; set; } = string.Empty;
    public string TransportDetails { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public InwardStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedOn { get; set; }
    public List<InwardItemDto> Items { get; set; } = new();
    public List<AttachmentDto> Attachments { get; set; } = new();
}

public class InwardItemLineRequest
{
    public Guid? ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string ItemMake { get; set; } = string.Empty;
    public string ItemModel { get; set; } = string.Empty;
    public string HsnCode { get; set; } = string.Empty;
    public string Unit { get; set; } = "Nos";
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public string Remarks { get; set; } = string.Empty;

    /// <summary>Serial numbers for serial-tracked items (manual or scanner entry).</summary>
    public List<string> Serials { get; set; } = new();
}

public class InwardSaveRequest
{
    public Guid? Id { get; set; }
    public DateTime InwardDate { get; set; } = DateTime.Today;
    public InwardType InwardType { get; set; } = InwardType.CustomerReturn;
    public Guid? CustomerId { get; set; }
    public Guid? VendorId { get; set; }
    public Guid? PurposeId { get; set; }
    public string ReferenceInvoiceNo { get; set; } = string.Empty;
    public DateTime? ReferenceInvoiceDate { get; set; }
    public string ChallanNo { get; set; } = string.Empty;
    public string TransportDetails { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public InwardStatus Status { get; set; } = InwardStatus.Received;
    public List<InwardItemLineRequest> Items { get; set; } = new();
}

public class InwardStatusRequest
{
    public Guid InwardId { get; set; }
    public InwardStatus Status { get; set; }
}

public class AttachmentDto
{
    public Guid Id { get; set; }
    public AttachmentEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime UploadedOn { get; set; }
    public string DisplaySize => FormatSize(FileSize);

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return $"{size:0.##} {units[unit]}";
    }
}
