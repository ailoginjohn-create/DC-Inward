using InwardDC.Domain.Enums;

namespace InwardDC.Domain.Criteria;

/// <summary>
/// Search filters are plain POCOs (no LINQ expressions) so the same object can be
/// translated into a REST API query string for a future remote repository.
/// </summary>
public class CustomerSearchFilter : PagedRequest
{
    public string? SearchText { get; set; }
    public bool? IsActive { get; set; }
    public bool IncludeDeleted { get; set; }
}

public class VendorSearchFilter : PagedRequest
{
    public string? SearchText { get; set; }
    public bool? IsActive { get; set; }
    public bool IncludeDeleted { get; set; }
}

public class ItemSearchFilter : PagedRequest
{
    public string? SearchText { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsActive { get; set; }
    public bool IncludeDeleted { get; set; }
}

public class ItemCategorySearchFilter : PagedRequest
{
    public string? SearchText { get; set; }
    public bool? IsActive { get; set; }
    public bool IncludeDeleted { get; set; }
}

/// <summary>
/// Inward search supports lookups by customer, serial number, model, date range,
/// invoice number, challan number and status — the "powerful search" requirement.
/// </summary>
public class InwardSearchFilter : PagedRequest
{
    public string? SearchText { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? VendorId { get; set; }
    public Guid? ItemId { get; set; }
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? InvoiceNo { get; set; }
    public string? ChallanNo { get; set; }
    public InwardStatus? Status { get; set; }
    public InwardType? InwardType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IncludeDeleted { get; set; }
}

public class DispatchSearchFilter : PagedRequest
{
    public string? SearchText { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ItemId { get; set; }
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? InvoiceNo { get; set; }
    public string? ChallanNo { get; set; }
    public DispatchStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IncludeDeleted { get; set; }
}

public class AuditLogFilter : PagedRequest
{
    public Guid? UserId { get; set; }
    public AuditAction? Action { get; set; }
    public string? EntityType { get; set; }
    public string? SearchText { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

/// <summary>Cross-module search for the global Search screen.</summary>
public class GlobalSearchFilter : PagedRequest
{
    public string Query { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? InvoiceNo { get; set; }
    public string? ChallanNo { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Status { get; set; }
}

public class UserSearchFilter : PagedRequest
{
    public string? SearchText { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
    public bool IncludeDeleted { get; set; }
}

public class ItemEventFilter : PagedRequest
{
    public Guid? ItemId { get; set; }
    public string? SerialNo { get; set; }
    public ItemEventType? EventType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class ReportPeriodFilter
{
    public DateTime FromDate { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    public DateTime ToDate { get; set; } = DateTime.Today;
    public Guid? CustomerId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? VendorId { get; set; }
    public bool IncludeCancelled { get; set; }
}
