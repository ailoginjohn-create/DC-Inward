using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Interfaces;
using InwardDC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InwardDC.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;

    public CustomerRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<Customer?> GetByCodeAsync(string code, CancellationToken ct = default)
        => _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code && !x.IsDeleted, ct);

    public async Task<PagedResult<Customer>> GetPagedAsync(CustomerSearchFilter filter, CancellationToken ct = default)
    {
        var query = _db.Customers.AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term)
                || x.ContactPerson.Contains(term) || x.Mobile.Contains(term) || x.Email.Contains(term));
        }

        if (filter.IsActive.HasValue)
            query = query.Where(x => x.IsActive == filter.IsActive.Value);

        query = (filter.SortBy?.ToLower()) switch
        {
            "code" => filter.SortDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            "name" => filter.SortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "city" => filter.SortDescending ? query.OrderByDescending(x => x.City) : query.OrderBy(x => x.City),
            _ => query.OrderBy(x => x.Name)
        };

        return await PagingHelper.ToPagedAsync(query, filter, ct);
    }

    public async Task<IReadOnlyList<Customer>> GetAllActiveAsync(CancellationToken ct = default)
        => await _db.Customers.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public Task<bool> CodeExistsAsync(string code, Guid? exceptId = null, CancellationToken ct = default)
    {
        var query = _db.Customers.AsNoTracking().Where(x => !x.IsDeleted && x.Code == code);
        if (exceptId.HasValue)
            query = query.Where(x => x.Id != exceptId.Value);
        return query.AnyAsync(ct);
    }

    public Task<int> CountAsync(CancellationToken ct = default)
        => _db.Customers.AsNoTracking().CountAsync(x => !x.IsDeleted, ct);

    public Task AddAsync(Customer customer, CancellationToken ct = default)
        => _db.Customers.AddAsync(customer, ct).AsTask();

    public void Update(Customer customer) => _db.UpdateTracked(customer);
}

public class VendorRepository : IVendorRepository
{
    private readonly AppDbContext _db;

    public VendorRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Vendor?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Vendors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<Vendor?> GetByCodeAsync(string code, CancellationToken ct = default)
        => _db.Vendors.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code && !x.IsDeleted, ct);

    public async Task<PagedResult<Vendor>> GetPagedAsync(VendorSearchFilter filter, CancellationToken ct = default)
    {
        var query = _db.Vendors.AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term)
                || x.ContactPerson.Contains(term) || x.Mobile.Contains(term) || x.Email.Contains(term));
        }

        if (filter.IsActive.HasValue)
            query = query.Where(x => x.IsActive == filter.IsActive.Value);

        query = (filter.SortBy?.ToLower()) switch
        {
            "code" => filter.SortDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            "name" => filter.SortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            _ => query.OrderBy(x => x.Name)
        };

        return await PagingHelper.ToPagedAsync(query, filter, ct);
    }

    public async Task<IReadOnlyList<Vendor>> GetAllActiveAsync(CancellationToken ct = default)
        => await _db.Vendors.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public Task<bool> CodeExistsAsync(string code, Guid? exceptId = null, CancellationToken ct = default)
    {
        var query = _db.Vendors.AsNoTracking().Where(x => !x.IsDeleted && x.Code == code);
        if (exceptId.HasValue)
            query = query.Where(x => x.Id != exceptId.Value);
        return query.AnyAsync(ct);
    }

    public Task<int> CountAsync(CancellationToken ct = default)
        => _db.Vendors.AsNoTracking().CountAsync(x => !x.IsDeleted, ct);

    public Task AddAsync(Vendor vendor, CancellationToken ct = default)
        => _db.Vendors.AddAsync(vendor, ct).AsTask();

    public void Update(Vendor vendor) => _db.UpdateTracked(vendor);
}

public class ItemRepository : IItemRepository
{
    private readonly AppDbContext _db;

    public ItemRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<Item?> GetByCodeAsync(string code, CancellationToken ct = default)
        => _db.Items.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code && !x.IsDeleted, ct);

    public Task<Item?> GetBySerialAsync(string serialNo, CancellationToken ct = default)
        => _db.Items.AsNoTracking().FirstOrDefaultAsync(
            x => !x.IsDeleted && _db.SerialNumbers.Any(s => !s.IsDeleted && s.ItemId == x.Id && s.SerialNo == serialNo), ct);

    public async Task<PagedResult<Item>> GetPagedAsync(ItemSearchFilter filter, CancellationToken ct = default)
    {
        var query = _db.Items.AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term)
                || x.Make.Contains(term) || x.Model.Contains(term) || x.HsnCode.Contains(term));
        }

        if (filter.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == filter.CategoryId.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(x => x.IsActive == filter.IsActive.Value);

        query = query.Include(x => x.Category);

        query = (filter.SortBy?.ToLower()) switch
        {
            "code" => filter.SortDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            "name" => filter.SortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "model" => filter.SortDescending ? query.OrderByDescending(x => x.Model) : query.OrderBy(x => x.Model),
            _ => query.OrderBy(x => x.Name)
        };

        return await PagingHelper.ToPagedAsync(query, filter, ct);
    }

    public async Task<IReadOnlyList<Item>> GetAllActiveAsync(CancellationToken ct = default)
        => await _db.Items.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public Task<bool> CodeExistsAsync(string code, Guid? exceptId = null, CancellationToken ct = default)
    {
        var query = _db.Items.AsNoTracking().Where(x => !x.IsDeleted && x.Code == code);
        if (exceptId.HasValue)
            query = query.Where(x => x.Id != exceptId.Value);
        return query.AnyAsync(ct);
    }

    public Task<bool> CategoryInUseAsync(Guid categoryId, CancellationToken ct = default)
        => _db.Items.AsNoTracking().AnyAsync(x => !x.IsDeleted && x.CategoryId == categoryId, ct);

    public Task<int> CountAsync(CancellationToken ct = default)
        => _db.Items.AsNoTracking().CountAsync(x => !x.IsDeleted, ct);

    public Task AddAsync(Item item, CancellationToken ct = default)
        => _db.Items.AddAsync(item, ct).AsTask();

    public void Update(Item item) => _db.UpdateTracked(item);
}

public class ItemCategoryRepository : IItemCategoryRepository
{
    private readonly AppDbContext _db;

    public ItemCategoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<ItemCategory?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.ItemCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<ItemCategory?> GetByCodeAsync(string code, CancellationToken ct = default)
        => _db.ItemCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code && !x.IsDeleted, ct);

    public async Task<PagedResult<ItemCategory>> GetPagedAsync(ItemCategorySearchFilter filter, CancellationToken ct = default)
    {
        var query = _db.ItemCategories.AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term));
        }

        if (filter.IsActive.HasValue)
            query = query.Where(x => x.IsActive == filter.IsActive.Value);

        query = (filter.SortBy?.ToLower()) switch
        {
            "code" => filter.SortDescending ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            "name" => filter.SortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            _ => query.OrderBy(x => x.Name)
        };

        return await PagingHelper.ToPagedAsync(query, filter, ct);
    }

    public async Task<IReadOnlyList<ItemCategory>> GetAllActiveAsync(CancellationToken ct = default)
        => await _db.ItemCategories.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public Task<bool> CodeExistsAsync(string code, Guid? exceptId = null, CancellationToken ct = default)
    {
        var query = _db.ItemCategories.AsNoTracking().Where(x => !x.IsDeleted && x.Code == code);
        if (exceptId.HasValue)
            query = query.Where(x => x.Id != exceptId.Value);
        return query.AnyAsync(ct);
    }

    public Task AddAsync(ItemCategory category, CancellationToken ct = default)
        => _db.ItemCategories.AddAsync(category, ct).AsTask();

    public void Update(ItemCategory category) => _db.UpdateTracked(category);
}

public class PurposeRepository : IPurposeRepository
{
    private readonly AppDbContext _db;

    public PurposeRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Purpose?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Purposes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<Purpose?> GetByNameAsync(string name, CancellationToken ct = default)
        => _db.Purposes.AsNoTracking().FirstOrDefaultAsync(x => x.Name == name && !x.IsDeleted, ct);

    public async Task<PagedResult<Purpose>> GetPagedAsync(PurposeSearchFilter filter, CancellationToken ct = default)
    {
        var query = _db.Purposes.AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(x => x.Name.Contains(term) || x.Description.Contains(term));
        }

        if (filter.IsActive.HasValue)
            query = query.Where(x => x.IsActive == filter.IsActive.Value);

        query = (filter.SortBy?.ToLower()) switch
        {
            "name" => filter.SortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            _ => query.OrderBy(x => x.Name)
        };

        return await PagingHelper.ToPagedAsync(query, filter, ct);
    }

    public async Task<IReadOnlyList<Purpose>> GetAllActiveAsync(CancellationToken ct = default)
        => await _db.Purposes.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    public Task<bool> NameExistsAsync(string name, Guid? exceptId = null, CancellationToken ct = default)
    {
        var query = _db.Purposes.AsNoTracking().Where(x => !x.IsDeleted && x.Name == name);
        if (exceptId.HasValue)
            query = query.Where(x => x.Id != exceptId.Value);
        return query.AnyAsync(ct);
    }

    public async Task<bool> IsInUseAsync(Guid purposeId, CancellationToken ct = default)
    {
        var inInward = await _db.InwardEntries.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.PurposeId == purposeId, ct);
        if (inInward)
            return true;
        return await _db.DispatchChallans.AsNoTracking()
            .AnyAsync(x => !x.IsDeleted && x.PurposeId == purposeId, ct);
    }

    public Task AddAsync(Purpose purpose, CancellationToken ct = default)
        => _db.Purposes.AddAsync(purpose, ct).AsTask();

    public void Update(Purpose purpose) => _db.UpdateTracked(purpose);
}
