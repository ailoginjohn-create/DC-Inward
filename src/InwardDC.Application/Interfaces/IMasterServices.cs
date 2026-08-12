using InwardDC.Application.DTOs;
using InwardDC.Domain.Criteria;

namespace InwardDC.Application.Interfaces;

public interface ICustomerService
{
    Task<PagedResponse<CustomerDto>> GetPagedAsync(CustomerSearchFilter filter, CancellationToken ct = default);
    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DropdownItemDto>> GetDropdownAsync(CancellationToken ct = default);
    Task<OperationResult> SaveAsync(CustomerSaveRequest request, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<string> GenerateCodeAsync(CancellationToken ct = default);
}

public interface IVendorService
{
    Task<PagedResponse<VendorDto>> GetPagedAsync(VendorSearchFilter filter, CancellationToken ct = default);
    Task<VendorDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DropdownItemDto>> GetDropdownAsync(CancellationToken ct = default);
    Task<OperationResult> SaveAsync(VendorSaveRequest request, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<string> GenerateCodeAsync(CancellationToken ct = default);
}

public interface IItemCategoryService
{
    Task<PagedResponse<ItemCategoryDto>> GetPagedAsync(ItemCategorySearchFilter filter, CancellationToken ct = default);
    Task<ItemCategoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DropdownItemDto>> GetDropdownAsync(CancellationToken ct = default);
    Task<OperationResult> SaveAsync(ItemCategorySaveRequest request, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<string> GenerateCodeAsync(CancellationToken ct = default);
}

public interface IPurposeService
{
    Task<PagedResponse<PurposeDto>> GetPagedAsync(PurposeSearchFilter filter, CancellationToken ct = default);
    Task<PurposeDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DropdownItemDto>> GetDropdownAsync(CancellationToken ct = default);
    Task<OperationResult> SaveAsync(PurposeSaveRequest request, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IItemService
{
    Task<PagedResponse<ItemDto>> GetPagedAsync(ItemSearchFilter filter, CancellationToken ct = default);
    Task<ItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DropdownItemDto>> GetDropdownAsync(CancellationToken ct = default);
    Task<OperationResult> SaveAsync(ItemSaveRequest request, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<string> GenerateCodeAsync(CancellationToken ct = default);
}
