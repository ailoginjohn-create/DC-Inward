using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Interfaces;
using InwardDC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InwardDC.Infrastructure.Repositories;

public class InwardRepository : IInwardRepository
{
    private readonly AppDbContext _db;

    public InwardRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<InwardEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.InwardEntries.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Vendor)
            .Include(x => x.Purpose)
            .Include(x => x.Items).ThenInclude(x => x.Item)
            .Include(x => x.Items).ThenInclude(x => x.Serials)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<InwardEntry?> GetByInwardNoAsync(string inwardNo, CancellationToken ct = default)
        => _db.InwardEntries.AsNoTracking().FirstOrDefaultAsync(x => x.InwardNo == inwardNo && !x.IsDeleted, ct);

    public async Task<PagedResult<InwardEntry>> GetPagedAsync(InwardSearchFilter filter, CancellationToken ct = default)
    {
        var query = _db.InwardEntries.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Vendor)
            .Include(x => x.Purpose)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(x => x.InwardNo.Contains(term)
                || x.ReferenceInvoiceNo.Contains(term)
                || x.ChallanNo.Contains(term)
                || x.Remarks.Contains(term)
                || (x.Customer != null && x.Customer.Name.Contains(term))
                || (x.Vendor != null && x.Vendor.Name.Contains(term)));
        }

        if (filter.CustomerId.HasValue)
            query = query.Where(x => x.CustomerId == filter.CustomerId.Value);
        if (filter.VendorId.HasValue)
            query = query.Where(x => x.VendorId == filter.VendorId.Value);
        if (filter.ItemId.HasValue)
            query = query.Where(x => x.Items.Any(i => i.ItemId == filter.ItemId.Value));
        if (!string.IsNullOrWhiteSpace(filter.SerialNumber))
            query = query.Where(x => x.Items.Any(i => i.Serials.Any(s => s.SerialNo.Contains(filter.SerialNumber))));
        if (!string.IsNullOrWhiteSpace(filter.Model))
            query = query.Where(x => x.Items.Any(i => i.ItemModel.Contains(filter.Model)));
        if (!string.IsNullOrWhiteSpace(filter.InvoiceNo))
            query = query.Where(x => x.ReferenceInvoiceNo.Contains(filter.InvoiceNo));
        if (!string.IsNullOrWhiteSpace(filter.ChallanNo))
            query = query.Where(x => x.ChallanNo.Contains(filter.ChallanNo));
        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);
        if (filter.InwardType.HasValue)
            query = query.Where(x => x.InwardType == filter.InwardType.Value);
        if (filter.FromDate.HasValue)
            query = query.Where(x => x.InwardDate >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue)
            query = query.Where(x => x.InwardDate <= filter.ToDate.Value.Date);

        query = (filter.SortBy?.ToLower()) switch
        {
            "inwardno" => filter.SortDescending ? query.OrderByDescending(x => x.InwardNo) : query.OrderBy(x => x.InwardNo),
            "date" => filter.SortDescending ? query.OrderByDescending(x => x.InwardDate) : query.OrderBy(x => x.InwardDate),
            "status" => filter.SortDescending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            _ => query.OrderByDescending(x => x.InwardDate)
        };

        return await PagingHelper.ToPagedAsync(query, filter, ct);
    }

    public async Task<IReadOnlyList<InwardEntry>> GetByPeriodAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.InwardEntries.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Vendor)
            .Include(x => x.Purpose)
            .Where(x => !x.IsDeleted && x.InwardDate >= from.Date && x.InwardDate <= to.Date)
            .OrderBy(x => x.InwardDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<InwardEntry>> GetByPeriodDetailedAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.InwardEntries.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Vendor)
            .Include(x => x.Purpose)
            .Include(x => x.Items).ThenInclude(x => x.Item)
            .Include(x => x.Items).ThenInclude(x => x.Serials)
            .Where(x => !x.IsDeleted && x.InwardDate >= from.Date && x.InwardDate <= to.Date)
            .OrderBy(x => x.InwardDate)
            .ToListAsync(ct);

    /// <summary>
    /// Returns inward lines that still have dispatchable quantity, with their in-stock
    /// serial numbers. Used by the DC generation module.
    /// </summary>
    public async Task<IReadOnlyList<InwardItem>> GetAvailableStockAsync(Guid? itemId = null, string? search = null, CancellationToken ct = default)
    {
        var query = _db.InwardItems.AsNoTracking()
            .Where(x => !x.IsDeleted
                && !x.InwardEntry!.IsDeleted
                && x.InwardEntry.Status != Domain.Enums.InwardStatus.Cancelled
                && x.Quantity > x.DispatchedQuantity);

        if (itemId.HasValue)
            query = query.Where(x => x.ItemId == itemId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.ItemName.Contains(term)
                || x.ItemModel.Contains(term)
                || x.ItemMake.Contains(term)
                || x.InwardEntry!.InwardNo.Contains(term));
        }

        return await query
            .Include(x => x.Item)
            .Include(x => x.InwardEntry)
            .Include(x => x.Serials.Where(s => !s.IsDeleted && s.Status == Domain.Enums.SerialStatus.InStock))
            .OrderBy(x => x.InwardEntry!.InwardDate)
            .ToListAsync(ct);
    }

    public async Task<InwardItem?> GetInwardItemAsync(Guid id, CancellationToken ct = default)
        => await _db.InwardItems.AsNoTracking()
            .Include(x => x.Item)
            .Include(x => x.Serials.Where(s => !s.IsDeleted && s.Status == Domain.Enums.SerialStatus.InStock))
            .Include(x => x.InwardEntry)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    /// <summary>Tracked load used by the edit path so EF can diff child collections.</summary>
    public Task<InwardEntry?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
        => _db.InwardEntries
            .Include(x => x.Items).ThenInclude(x => x.Serials)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    /// <summary>Tracked load of an inward line (and its in-stock serials) for dispatch allocation.</summary>
    public Task<InwardItem?> GetInwardItemForUpdateAsync(Guid id, CancellationToken ct = default)
        => _db.InwardItems
            .Include(x => x.Item)
            .Include(x => x.Serials.Where(s => !s.IsDeleted && s.Status == Domain.Enums.SerialStatus.InStock))
            .Include(x => x.InwardEntry)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<int> CountAsync(CancellationToken ct = default)
        => _db.InwardEntries.AsNoTracking().CountAsync(x => !x.IsDeleted, ct);

    public Task AddAsync(InwardEntry entry, CancellationToken ct = default)
        => _db.InwardEntries.AddAsync(entry, ct).AsTask();

    public void Update(InwardEntry entry) => _db.UpdateTracked(entry);

    public Task<bool> IsDispatchedAsync(Guid inwardEntryId, CancellationToken ct = default)
        => _db.DispatchChallans.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.SourceInwardEntryId == inwardEntryId
                && x.Status != Domain.Enums.DispatchStatus.Cancelled, ct);
}

public class DCRepository : IDCRepository
{
    private readonly AppDbContext _db;

    public DCRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<DispatchChallan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.DispatchChallans.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Purpose)
            .Include(x => x.SourceInwardEntry)
            .Include(x => x.Items).ThenInclude(x => x.Item)
            .Include(x => x.Items).ThenInclude(x => x.Serials)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<DispatchChallan?> GetByDcNoAsync(string dcNo, CancellationToken ct = default)
        => _db.DispatchChallans.AsNoTracking().FirstOrDefaultAsync(x => x.DcNo == dcNo && !x.IsDeleted, ct);

    /// <summary>Tracked load with items + source inward lines + serials for cancel/reverse.</summary>
    public Task<DispatchChallan?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
        => _db.DispatchChallans
            .Include(x => x.SourceInwardEntry)
            .Include(x => x.Items).ThenInclude(x => x.SourceInwardItem)
            .Include(x => x.Items).ThenInclude(x => x.Serials)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<PagedResult<DispatchChallan>> GetPagedAsync(DispatchSearchFilter filter, CancellationToken ct = default)
    {
        var query = _db.DispatchChallans.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Purpose)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(x => x.DcNo.Contains(term)
                || x.ReferenceChallanNo.Contains(term)
                || x.Remarks.Contains(term)
                || x.Customer != null && x.Customer.Name.Contains(term));
        }

        if (filter.CustomerId.HasValue)
            query = query.Where(x => x.CustomerId == filter.CustomerId.Value);
        if (filter.ItemId.HasValue)
            query = query.Where(x => x.Items.Any(i => i.ItemId == filter.ItemId.Value));
        if (!string.IsNullOrWhiteSpace(filter.SerialNumber))
            query = query.Where(x => x.Items.Any(i => i.Serials.Any(s => s.SerialNo.Contains(filter.SerialNumber))));
        if (!string.IsNullOrWhiteSpace(filter.Model))
            query = query.Where(x => x.Items.Any(i => i.ItemModel.Contains(filter.Model)));
        if (!string.IsNullOrWhiteSpace(filter.InvoiceNo))
            query = query.Where(x => x.SourceInwardEntry != null && x.SourceInwardEntry.ReferenceInvoiceNo.Contains(filter.InvoiceNo));
        if (!string.IsNullOrWhiteSpace(filter.ChallanNo))
            query = query.Where(x => x.ReferenceChallanNo.Contains(filter.ChallanNo));
        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);
        if (filter.FromDate.HasValue)
            query = query.Where(x => x.DcDate >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue)
            query = query.Where(x => x.DcDate <= filter.ToDate.Value.Date);

        query = (filter.SortBy?.ToLower()) switch
        {
            "dcno" => filter.SortDescending ? query.OrderByDescending(x => x.DcNo) : query.OrderBy(x => x.DcNo),
            "date" => filter.SortDescending ? query.OrderByDescending(x => x.DcDate) : query.OrderBy(x => x.DcDate),
            "status" => filter.SortDescending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            _ => query.OrderByDescending(x => x.DcDate)
        };

        return await PagingHelper.ToPagedAsync(query, filter, ct);
    }

    public async Task<IReadOnlyList<DispatchChallan>> GetByPeriodAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.DispatchChallans.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Purpose)
            .Where(x => !x.IsDeleted && x.DcDate >= from.Date && x.DcDate <= to.Date)
            .OrderBy(x => x.DcDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DispatchChallan>> GetByPeriodDetailedAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => await _db.DispatchChallans.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Purpose)
            .Include(x => x.SourceInwardEntry)
            .Include(x => x.Items).ThenInclude(x => x.Item)
            .Include(x => x.Items).ThenInclude(x => x.Serials)
            .Where(x => !x.IsDeleted && x.DcDate >= from.Date && x.DcDate <= to.Date)
            .OrderBy(x => x.DcDate)
            .ToListAsync(ct);

    public Task<int> CountAsync(CancellationToken ct = default)
        => _db.DispatchChallans.AsNoTracking().CountAsync(x => !x.IsDeleted, ct);

    public Task AddAsync(DispatchChallan challan, CancellationToken ct = default)
        => _db.DispatchChallans.AddAsync(challan, ct).AsTask();

    public void Update(DispatchChallan challan) => _db.UpdateTracked(challan);

    public Task<Guid?> FindDcByInwardAsync(Guid inwardEntryId, CancellationToken ct = default)
        => _db.DispatchChallans.AsNoTracking()
            .Where(x => !x.IsDeleted && x.SourceInwardEntryId == inwardEntryId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct);
}
