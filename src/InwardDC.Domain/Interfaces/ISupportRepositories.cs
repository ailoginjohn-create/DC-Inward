using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;

namespace InwardDC.Domain.Interfaces;

/// <summary>Attachment metadata data access contract.</summary>
public interface IAttachmentRepository
{
    Task<Attachment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Attachment>> GetByEntityAsync(AttachmentEntityType entityType, Guid entityId, CancellationToken ct = default);
    Task AddAsync(Attachment attachment, CancellationToken ct = default);
    void Remove(Attachment attachment);
}

/// <summary>Audit trail data access contract.</summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<PagedResult<AuditLog>> GetPagedAsync(AuditLogFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}

/// <summary>Key/value settings data access contract.</summary>
public interface ISettingRepository
{
    Task<Setting?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<Setting>> GetByGroupAsync(string group, CancellationToken ct = default);
    Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Setting setting, CancellationToken ct = default);
    void Update(Setting setting);
    Task<string?> GetValueAsync(string key, CancellationToken ct = default);
    Task<bool> KeyExistsAsync(string key, CancellationToken ct = default);
}

/// <summary>Auto-number sequence counter data access contract.</summary>
public interface ISequenceRepository
{
    /// <summary>Atomically increments and returns the next human-readable number for the entity/period.</summary>
    Task<long> GetNextAsync(string entityName, string prefix, int year, CancellationToken ct = default);
    Task<SequenceCounter?> GetCurrentAsync(string entityName, string prefix, int year, CancellationToken ct = default);
}

/// <summary>Item lifecycle event data access contract (history / timeline).</summary>
public interface IItemEventRepository
{
    Task AddAsync(ItemEvent itemEvent, CancellationToken ct = default);
    Task<IReadOnlyList<ItemEvent>> GetByItemAsync(Guid itemId, CancellationToken ct = default);
    Task<IReadOnlyList<ItemEvent>> GetBySerialAsync(string serialNo, CancellationToken ct = default);
    Task<PagedResult<ItemEvent>> GetPagedAsync(ItemEventFilter filter, CancellationToken ct = default);
}

/// <summary>Physical serial number record data access contract.</summary>
public interface ISerialNumberRepository
{
    Task<SerialNumber?> GetBySerialAsync(string serialNo, CancellationToken ct = default);
    Task<IReadOnlyList<SerialNumber>> GetByItemAsync(Guid itemId, CancellationToken ct = default);
    Task<IReadOnlyList<SerialNumber>> GetInStockByItemAsync(Guid itemId, CancellationToken ct = default);
    Task<IReadOnlyList<SerialNumber>> GetByInwardAsync(Guid inwardEntryId, CancellationToken ct = default);
    Task<IReadOnlyList<SerialNumber>> GetAllWithDetailsAsync(Guid? itemId = null, string? search = null, CancellationToken ct = default);
    Task<int> CountInStockAsync(CancellationToken ct = default);
    Task<bool> SerialExistsAsync(string serialNo, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetExistingSerialsAsync(IReadOnlyCollection<string> serialNos, CancellationToken ct = default);
    Task<Dictionary<string, SerialNumber>> GetSerialsByNosAsync(IReadOnlyCollection<string> serialNos, CancellationToken ct = default);
    Task AddAsync(SerialNumber serial, CancellationToken ct = default);
    void Update(SerialNumber serial);
}

/// <summary>
/// Unit of work grouping all repositories. Exposes a transaction scope so multi
/// entity operations (e.g., DC generation updating inward + serials + events) are
/// atomic regardless of the underlying database provider.
/// </summary>
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    ICustomerRepository Customers { get; }
    IVendorRepository Vendors { get; }
    IItemRepository Items { get; }
    IItemCategoryRepository ItemCategories { get; }
    IPurposeRepository Purposes { get; }
    IInwardRepository Inwards { get; }
    IDCRepository DCs { get; }
    IAttachmentRepository Attachments { get; }
    IAuditLogRepository AuditLogs { get; }
    ISettingRepository Settings { get; }
    ISequenceRepository Sequences { get; }
    IItemEventRepository ItemEvents { get; }
    ISerialNumberRepository SerialNumbers { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);
}
