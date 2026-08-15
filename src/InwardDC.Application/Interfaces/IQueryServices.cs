using InwardDC.Application.DTOs;
using InwardDC.Domain.Criteria;
using InwardDC.Domain.Enums;

namespace InwardDC.Application.Interfaces;

/// <summary>Global / item / serial search contract.</summary>
public interface ISearchService
{
    Task<SearchResultDto> GlobalSearchAsync(GlobalSearchFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<ItemHistoryDto>> GetItemHistoryAsync(Guid itemId, CancellationToken ct = default);
    Task<IReadOnlyList<ItemHistoryDto>> GetSerialHistoryAsync(string serialNo, CancellationToken ct = default);
    Task<IReadOnlyList<ItemStockDto>> GetStockReportAsync(Guid? itemId = null, string? search = null, CancellationToken ct = default);
    Task<IReadOnlyList<ItemStockDto>> GetSerialLookupAsync(string serialNo, CancellationToken ct = default);
}

/// <summary>Reporting contract (daily, monthly, customer-wise, item-wise).</summary>
public interface IReportService
{
    Task<IReadOnlyList<DailySummaryDto>> GetDailySummaryAsync(ReportPeriodFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<MonthlySummaryDto>> GetMonthlySummaryAsync(ReportPeriodFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerWiseSummaryDto>> GetCustomerWiseAsync(ReportPeriodFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<ItemWiseSummaryDto>> GetItemWiseAsync(ReportPeriodFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<ReportRowDto>> GetInwardDetailAsync(ReportPeriodFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<ReportRowDto>> GetDispatchDetailAsync(ReportPeriodFilter filter, CancellationToken ct = default);
}

public interface IAuditService
{
    Task AddAsync(AuditAction action, string entityType, Guid? entityId, string description, string details = "", CancellationToken ct = default);
    Task<PagedResponse<AuditLogDto>> GetPagedAsync(AuditLogFilter filter, CancellationToken ct = default);
}

public interface ISettingsService
{
    Task<CompanySettingsDto> GetCompanySettingsAsync(CancellationToken ct = default);
    Task<OperationResult> SaveCompanySettingsAsync(CompanySettingsDto settings, CancellationToken ct = default);
    Task<IReadOnlyList<SettingDto>> GetAllAsync(CancellationToken ct = default);
    Task<OperationResult> SetAsync(string key, string value, CancellationToken ct = default);
    Task<string> GetValueAsync(string key, string defaultValue = "", CancellationToken ct = default);
}

public interface IAttachmentService
{
    Task<AttachmentDto> AttachAsync(AttachmentEntityType entityType, Guid entityId, string sourceFilePath, string? notes = null, CancellationToken ct = default);
    Task<IReadOnlyList<AttachmentDto>> GetByEntityAsync(AttachmentEntityType entityType, Guid entityId, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid attachmentId, CancellationToken ct = default);
    Task<string?> GetFullPathAsync(Guid attachmentId, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default);
}

public interface IExcelService
{
    Task<FileImportResult> ImportInwardAsync(Stream stream, string fileName, CancellationToken ct = default);
    Task<FileImportResult> ImportDispatchesAsync(Stream stream, string fileName, CancellationToken ct = default);
    Task<Stream> CreateImportTemplateAsync(CancellationToken ct = default);
    Task<Stream> CreateDispatchImportTemplateAsync(CancellationToken ct = default);
    Task<string> ExportInwardsAsync(InwardSearchFilter filter, string filePath, CancellationToken ct = default);
    Task<string> ExportDispatchesAsync(DispatchSearchFilter filter, string filePath, CancellationToken ct = default);
    Task<string> ExportReportAsync(string title, IReadOnlyList<ReportRowDto> rows, string filePath, CancellationToken ct = default);
    Task<string> ExportAuditLogsAsync(IReadOnlyList<AuditLogDto> rows, string filePath, CancellationToken ct = default);
}

public interface IPdfService
{
    Task<string> GenerateDcPdfAsync(Guid dispatchId, string outputPath, CancellationToken ct = default);
    Task<string> GenerateInwardPdfAsync(Guid inwardId, string outputPath, CancellationToken ct = default);
    Task<string> GenerateReportPdfAsync(string title, IReadOnlyList<ReportRowDto> rows, string outputPath, CancellationToken ct = default);
}

public interface IBackupService
{
    Task<string> CreateBackupAsync(CancellationToken ct = default);
    Task<OperationResult> RestoreAsync(string zipPath, CancellationToken ct = default);
    Task<OperationResult> FactoryResetAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListBackupsAsync(CancellationToken ct = default);
}
