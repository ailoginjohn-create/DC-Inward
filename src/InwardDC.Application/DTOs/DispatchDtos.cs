using InwardDC.Domain.Enums;

namespace InwardDC.Application.DTOs;

public class DispatchItemDto
{
    public Guid Id { get; set; }
    public Guid? SourceInwardItemId { get; set; }
    public string SourceInwardNo { get; set; } = string.Empty;
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
    public List<string> Serials { get; set; } = new();
}

public class DispatchDto
{
    public Guid Id { get; set; }
    public string DcNo { get; set; } = string.Empty;
    public DateTime DcDate { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid? SourceInwardEntryId { get; set; }
    public string SourceInwardNo { get; set; } = string.Empty;
    public string ReferenceChallanNo { get; set; } = string.Empty;
    public string TransportDetails { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public DispatchStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedOn { get; set; }
    public List<DispatchItemDto> Items { get; set; } = new();
    public List<AttachmentDto> Attachments { get; set; } = new();
}

public class DispatchLineRequest
{
    public Guid? SourceInwardItemId { get; set; }
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
    public List<string> Serials { get; set; } = new();
}

public class DispatchSaveRequest
{
    public Guid? Id { get; set; }
    public DateTime DcDate { get; set; } = DateTime.Today;
    public Guid CustomerId { get; set; }
    public Guid? SourceInwardEntryId { get; set; }
    public string ReferenceChallanNo { get; set; } = string.Empty;
    public string TransportDetails { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public List<DispatchLineRequest> Items { get; set; } = new();
}

/// <summary>Available (undispatched) stock of an item, used by the DC line picker.</summary>
public class AvailableStockDto
{
    public Guid InwardItemId { get; set; }
    public string InwardNo { get; set; } = string.Empty;
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal AvailableQuantity { get; set; }
    public decimal Rate { get; set; }
    public List<string> AvailableSerials { get; set; } = new();
}
