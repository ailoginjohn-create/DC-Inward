using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;

namespace InwardDC.Domain.Interfaces;

/// <summary>Inward entry (goods receipt) data access contract.</summary>
public interface IInwardRepository
{
    Task<InwardEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<InwardEntry?> GetByInwardNoAsync(string inwardNo, CancellationToken ct = default);
    Task<PagedResult<InwardEntry>> GetPagedAsync(InwardSearchFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<InwardEntry>> GetByPeriodAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<InwardEntry>> GetByPeriodDetailedAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<InwardItem>> GetAvailableStockAsync(Guid? itemId = null, string? search = null, CancellationToken ct = default);
    Task<InwardItem?> GetInwardItemAsync(Guid id, CancellationToken ct = default);
    Task<InwardItem?> GetInwardItemForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<InwardEntry?> GetForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task AddAsync(InwardEntry entry, CancellationToken ct = default);
    void Update(InwardEntry entry);
    Task<bool> IsDispatchedAsync(Guid inwardEntryId, CancellationToken ct = default);
}

/// <summary>Dispatch Challan (DC) data access contract.</summary>
public interface IDCRepository
{
    Task<DispatchChallan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DispatchChallan?> GetByDcNoAsync(string dcNo, CancellationToken ct = default);
    Task<PagedResult<DispatchChallan>> GetPagedAsync(DispatchSearchFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<DispatchChallan>> GetByPeriodAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<DispatchChallan>> GetByPeriodDetailedAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<DispatchChallan?> GetForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task AddAsync(DispatchChallan challan, CancellationToken ct = default);
    void Update(DispatchChallan challan);
    Task<Guid?> FindDcByInwardAsync(Guid inwardEntryId, CancellationToken ct = default);
}
