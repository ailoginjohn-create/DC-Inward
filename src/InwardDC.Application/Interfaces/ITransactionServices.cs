using InwardDC.Application.DTOs;
using InwardDC.Domain.Criteria;

namespace InwardDC.Application.Interfaces;

/// <summary>Inward entry (goods receipt) business contract.</summary>
public interface IInwardService
{
    Task<PagedResponse<InwardDto>> GetPagedAsync(InwardSearchFilter filter, CancellationToken ct = default);
    Task<InwardDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<string> PreviewNextNumberAsync(CancellationToken ct = default);
    Task<OperationResult> SaveAsync(InwardSaveRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateStatusAsync(InwardStatusRequest request, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Dispatch Challan (DC) business contract.</summary>
public interface IDispatchService
{
    Task<PagedResponse<DispatchDto>> GetPagedAsync(DispatchSearchFilter filter, CancellationToken ct = default);
    Task<DispatchDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AvailableStockDto>> GetAvailableStockAsync(Guid? itemId = null, string? search = null, CancellationToken ct = default);
    Task<string> PreviewNextNumberAsync(CancellationToken ct = default);
    Task<OperationResult> SaveAsync(DispatchSaveRequest request, CancellationToken ct = default);
    Task<OperationResult> CancelAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
