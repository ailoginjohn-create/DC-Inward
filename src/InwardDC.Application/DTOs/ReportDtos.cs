using InwardDC.Domain.Enums;

namespace InwardDC.Application.DTOs;

public class DashboardStatsDto
{
    public int TotalCustomers { get; set; }
    public int TotalVendors { get; set; }
    public int TotalItems { get; set; }
    public int TotalInwardEntries { get; set; }
    public int InwardThisMonth { get; set; }
    public decimal InwardAmountThisMonth { get; set; }
    public int TotalDcs { get; set; }
    public int DcsThisMonth { get; set; }
    public decimal DcAmountThisMonth { get; set; }
    public int ItemsInStock { get; set; }
    public int PendingDispatch { get; set; }
    public IReadOnlyList<RecentActivityDto> RecentInwards { get; set; } = Array.Empty<RecentActivityDto>();
    public IReadOnlyList<RecentActivityDto> RecentDcs { get; set; } = Array.Empty<RecentActivityDto>();
}

public class RecentActivityDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Party { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ReportRowDto
{
    public DateTime Date { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Party { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string SerialNo { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class DailySummaryDto
{
    public DateTime Date { get; set; }
    public int InwardCount { get; set; }
    public decimal InwardAmount { get; set; }
    public int DcCount { get; set; }
    public decimal DcAmount { get; set; }
}

public class MonthlySummaryDto
{
    public string YearMonth { get; set; } = string.Empty;
    public int InwardCount { get; set; }
    public decimal InwardAmount { get; set; }
    public int DcCount { get; set; }
    public decimal DcAmount { get; set; }
}

public class CustomerWiseSummaryDto
{
    public string PartyName { get; set; } = string.Empty;
    public int InwardCount { get; set; }
    public int InwardUnits { get; set; }
    public decimal InwardAmount { get; set; }
    public int DcCount { get; set; }
    public int DcUnits { get; set; }
    public decimal DcAmount { get; set; }
}

public class ItemWiseSummaryDto
{
    public string ItemName { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int InwardUnits { get; set; }
    public decimal InwardAmount { get; set; }
    public int DispatchedUnits { get; set; }
    public decimal DispatchedAmount { get; set; }
    public int InStock { get; set; }
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string ActionName => Action.ToString();
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class CompanySettingsDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyAddressLine1 { get; set; } = string.Empty;
    public string CompanyAddressLine2 { get; set; } = string.Empty;
    public string CompanyCity { get; set; } = string.Empty;
    public string CompanyState { get; set; } = string.Empty;
    public string CompanyPincode { get; set; } = string.Empty;
    public string CompanyPhone { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string CompanyGSTIN { get; set; } = string.Empty;
    public string CompanyPAN { get; set; } = string.Empty;
    public string CompanyLogoPath { get; set; } = string.Empty;
    public string InwardNumberPrefix { get; set; } = "INW";
    public string DcNumberPrefix { get; set; } = "DC";
    public string CustomerNumberPrefix { get; set; } = "CUS";
    public string VendorNumberPrefix { get; set; } = "VEN";
    public string ItemNumberPrefix { get; set; } = "ITM";
    public string CategoryNumberPrefix { get; set; } = "CAT";
    public string FooterNote { get; set; } = string.Empty;
    public bool RequireSerialForTrackedItems { get; set; } = true;
}

public class SettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public bool IsSystem { get; set; }
}
