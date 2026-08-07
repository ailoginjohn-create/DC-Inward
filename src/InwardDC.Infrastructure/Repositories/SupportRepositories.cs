using System.Data;
using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Interfaces;
using InwardDC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace InwardDC.Infrastructure.Repositories;

public class AttachmentRepository : IAttachmentRepository
{
    private readonly AppDbContext _db;

    public AttachmentRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Attachment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Attachments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Attachment>> GetByEntityAsync(AttachmentEntityType entityType, Guid entityId, CancellationToken ct = default)
        => await _db.Attachments.AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .OrderByDescending(x => x.UploadedOn)
            .ToListAsync(ct);

    public Task AddAsync(Attachment attachment, CancellationToken ct = default)
        => _db.Attachments.AddAsync(attachment, ct).AsTask();

    public void Remove(Attachment attachment) => _db.Attachments.Remove(attachment);
}

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _db;

    public AuditLogRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(AuditLog log, CancellationToken ct = default)
        => _db.AuditLogs.AddAsync(log, ct).AsTask();

    public async Task<PagedResult<AuditLog>> GetPagedAsync(AuditLogFilter filter, CancellationToken ct = default)
    {
        var query = _db.AuditLogs.AsNoTracking();

        if (filter.UserId.HasValue)
            query = query.Where(x => x.UserId == filter.UserId.Value);
        if (filter.Action.HasValue)
            query = query.Where(x => x.Action == filter.Action.Value);
        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(x => x.EntityType == filter.EntityType);
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(x => x.Description.Contains(term)
                || x.UserName.Contains(term)
                || x.FullName.Contains(term)
                || x.EntityType.Contains(term));
        }
        if (filter.FromDate.HasValue)
            query = query.Where(x => x.Timestamp >= filter.FromDate.Value.ToUniversalTime());
        if (filter.ToDate.HasValue)
            query = query.Where(x => x.Timestamp <= filter.ToDate.Value.Date.AddDays(1).ToUniversalTime());

        query = filter.SortDescending ? query.OrderByDescending(x => x.Timestamp) : query.OrderBy(x => x.Timestamp);

        return await PagingHelper.ToPagedAsync(query, filter, ct);
    }

    public async Task<IReadOnlyList<AuditLog>> GetRangeAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.AuditLogs.AsNoTracking()
            .Where(x => x.Timestamp >= from.ToUniversalTime() && x.Timestamp <= to.Date.AddDays(1).ToUniversalTime())
            .OrderBy(x => x.Timestamp)
            .ToListAsync(ct);

    public Task<int> CountAsync(CancellationToken ct = default)
        => _db.AuditLogs.AsNoTracking().CountAsync(ct);
}

public class SettingRepository : ISettingRepository
{
    private readonly AppDbContext _db;

    public SettingRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Setting?> GetByKeyAsync(string key, CancellationToken ct = default)
        => _db.Settings.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, ct);

    public async Task<IReadOnlyList<Setting>> GetByGroupAsync(string group, CancellationToken ct = default)
        => await _db.Settings.AsNoTracking()
            .Where(x => x.Group == group)
            .OrderBy(x => x.Key)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Setting>> GetAllAsync(CancellationToken ct = default)
        => await _db.Settings.AsNoTracking()
            .OrderBy(x => x.Group).ThenBy(x => x.Key)
            .ToListAsync(ct);

    public Task AddAsync(Setting setting, CancellationToken ct = default)
        => _db.Settings.AddAsync(setting, ct).AsTask();

    public void Update(Setting setting) => _db.UpdateTracked(setting);

    public async Task<string?> GetValueAsync(string key, CancellationToken ct = default)
    {
        var setting = await _db.Settings.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, ct);
        return setting?.Value;
    }

    public Task<bool> KeyExistsAsync(string key, CancellationToken ct = default)
        => _db.Settings.AsNoTracking().AnyAsync(x => x.Key == key, ct);
}

public class SequenceRepository : ISequenceRepository
{
    private readonly AppDbContext _db;

    public SequenceRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<long> GetNextAsync(string entityName, string prefix, int year, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var counter = await _db.SequenceCounters
            .FirstOrDefaultAsync(s => s.EntityName == entityName && s.Prefix == prefix && s.Year == year, ct);

        if (counter is null)
        {
            counter = new SequenceCounter
            {
                EntityName = entityName,
                Prefix = prefix,
                Year = year,
                LastNumber = 0
            };
            await _db.SequenceCounters.AddAsync(counter, ct);
        }

        counter.LastNumber += 1;
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return counter.LastNumber;
    }

    public Task<SequenceCounter?> GetCurrentAsync(string entityName, string prefix, int year, CancellationToken ct = default)
        => _db.SequenceCounters.AsNoTracking()
            .FirstOrDefaultAsync(s => s.EntityName == entityName && s.Prefix == prefix && s.Year == year, ct);
}

public class ItemEventRepository : IItemEventRepository
{
    private readonly AppDbContext _db;

    public ItemEventRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(ItemEvent itemEvent, CancellationToken ct = default)
        => _db.ItemEvents.AddAsync(itemEvent, ct).AsTask();

    public async Task<IReadOnlyList<ItemEvent>> GetByItemAsync(Guid itemId, CancellationToken ct = default)
        => await _db.ItemEvents.AsNoTracking()
            .Where(x => x.ItemId == itemId)
            .OrderByDescending(x => x.EventedOn)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ItemEvent>> GetBySerialAsync(string serialNo, CancellationToken ct = default)
        => await _db.ItemEvents.AsNoTracking()
            .Where(x => x.SerialNo == serialNo)
            .OrderByDescending(x => x.EventedOn)
            .ToListAsync(ct);

    public async Task<PagedResult<ItemEvent>> GetPagedAsync(ItemEventFilter filter, CancellationToken ct = default)
    {
        var query = _db.ItemEvents.AsNoTracking();

        if (filter.ItemId.HasValue)
            query = query.Where(x => x.ItemId == filter.ItemId.Value);
        if (!string.IsNullOrWhiteSpace(filter.SerialNo))
            query = query.Where(x => x.SerialNo == filter.SerialNo);
        if (filter.EventType.HasValue)
            query = query.Where(x => x.EventType == filter.EventType.Value);
        if (filter.FromDate.HasValue)
            query = query.Where(x => x.EventedOn >= filter.FromDate.Value.ToUniversalTime());
        if (filter.ToDate.HasValue)
            query = query.Where(x => x.EventedOn <= filter.ToDate.Value.Date.AddDays(1).ToUniversalTime());

        query = query.OrderByDescending(x => x.EventedOn);

        return await PagingHelper.ToPagedAsync(query, filter, ct);
    }
}

public class SerialNumberRepository : ISerialNumberRepository
{
    private readonly AppDbContext _db;

    public SerialNumberRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<SerialNumber?> GetBySerialAsync(string serialNo, CancellationToken ct = default)
        => _db.SerialNumbers.AsNoTracking().FirstOrDefaultAsync(x => x.SerialNo == serialNo && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<SerialNumber>> GetByItemAsync(Guid itemId, CancellationToken ct = default)
        => await _db.SerialNumbers.AsNoTracking()
            .Where(x => x.ItemId == itemId && !x.IsDeleted)
            .OrderBy(x => x.SerialNo)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SerialNumber>> GetInStockByItemAsync(Guid itemId, CancellationToken ct = default)
        => await _db.SerialNumbers.AsNoTracking()
            .Where(x => x.ItemId == itemId && !x.IsDeleted && x.Status == SerialStatus.InStock)
            .OrderBy(x => x.SerialNo)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SerialNumber>> GetByInwardAsync(Guid inwardEntryId, CancellationToken ct = default)
        => await _db.SerialNumbers.AsNoTracking()
            .Where(x => x.InwardEntryId == inwardEntryId && !x.IsDeleted)
            .OrderBy(x => x.SerialNo)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SerialNumber>> GetAllWithDetailsAsync(Guid? itemId = null, string? search = null, CancellationToken ct = default)
    {
        var query = _db.SerialNumbers.AsNoTracking()
            .Include(x => x.Item)
            .Include(x => x.InwardEntry)
            .Include(x => x.DispatchChallan).ThenInclude(x => x!.Customer)
            .Where(x => !x.IsDeleted);

        if (itemId.HasValue)
            query = query.Where(x => x.ItemId == itemId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.SerialNo.Contains(term)
                || x.Item != null && x.Item.Name.Contains(term)
                || x.Item != null && x.Item.Model.Contains(term)
                || x.InwardEntry != null && x.InwardEntry.InwardNo.Contains(term)
                || x.DispatchChallan != null && x.DispatchChallan.DcNo.Contains(term));
        }

        return await query.OrderBy(x => x.SerialNo).ToListAsync(ct);
    }

    public Task<int> CountInStockAsync(CancellationToken ct = default)
        => _db.SerialNumbers.AsNoTracking().CountAsync(x => !x.IsDeleted && x.Status == SerialStatus.InStock, ct);

    public Task<bool> SerialExistsAsync(string serialNo, CancellationToken ct = default)
        => _db.SerialNumbers.AsNoTracking().AnyAsync(x => x.SerialNo == serialNo && !x.IsDeleted, ct);

    public Task AddAsync(SerialNumber serial, CancellationToken ct = default)
        => _db.SerialNumbers.AddAsync(serial, ct).AsTask();

    public void Update(SerialNumber serial) => _db.UpdateTracked(serial);
}
