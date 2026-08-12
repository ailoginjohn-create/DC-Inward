using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;

namespace InwardDC.Domain.Interfaces;

/// <summary>Customer master data access contract.</summary>
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Customer?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<PagedResult<Customer>> GetPagedAsync(CustomerSearchFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> GetAllActiveAsync(CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, Guid? exceptId = null, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
    void Update(Customer customer);
}

/// <summary>Vendor master data access contract.</summary>
public interface IVendorRepository
{
    Task<Vendor?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Vendor?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<PagedResult<Vendor>> GetPagedAsync(VendorSearchFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<Vendor>> GetAllActiveAsync(CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, Guid? exceptId = null, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task AddAsync(Vendor vendor, CancellationToken ct = default);
    void Update(Vendor vendor);
}

/// <summary>Item master data access contract.</summary>
public interface IItemRepository
{
    Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Item?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<Item?> GetBySerialAsync(string serialNo, CancellationToken ct = default);
    Task<PagedResult<Item>> GetPagedAsync(ItemSearchFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<Item>> GetAllActiveAsync(CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, Guid? exceptId = null, CancellationToken ct = default);
    Task<bool> CategoryInUseAsync(Guid categoryId, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task AddAsync(Item item, CancellationToken ct = default);
    void Update(Item item);
}

/// <summary>Item category data access contract.</summary>
public interface IItemCategoryRepository
{
    Task<ItemCategory?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ItemCategory?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<PagedResult<ItemCategory>> GetPagedAsync(ItemCategorySearchFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<ItemCategory>> GetAllActiveAsync(CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, Guid? exceptId = null, CancellationToken ct = default);
    Task AddAsync(ItemCategory category, CancellationToken ct = default);
    void Update(ItemCategory category);
}

/// <summary>Inward/DC purpose master data access contract.</summary>
public interface IPurposeRepository
{
    Task<Purpose?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Purpose?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<PagedResult<Purpose>> GetPagedAsync(PurposeSearchFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<Purpose>> GetAllActiveAsync(CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, Guid? exceptId = null, CancellationToken ct = default);
    Task<bool> IsInUseAsync(Guid purposeId, CancellationToken ct = default);
    Task AddAsync(Purpose purpose, CancellationToken ct = default);
    void Update(Purpose purpose);
}
