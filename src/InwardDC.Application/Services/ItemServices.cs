using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Exceptions;
using InwardDC.Domain.Interfaces;

namespace InwardDC.Application.Services;

public class ItemCategoryService : IItemCategoryService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly ISettingsService _settings;

    public ItemCategoryService(IUnitOfWork uow, ICurrentUserService currentUser, IAuditService audit, ISettingsService settings)
    {
        _uow = uow;
        _currentUser = currentUser;
        _audit = audit;
        _settings = settings;
    }

    public async Task<PagedResponse<ItemCategoryDto>> GetPagedAsync(ItemCategorySearchFilter filter, CancellationToken ct = default)
    {
        var result = await _uow.ItemCategories.GetPagedAsync(filter, ct);
        var page = new PagedResult<ItemCategoryDto>
        {
            Items = result.Items.Select(ToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
        return PagedResponse<ItemCategoryDto>.From(page);
    }

    public async Task<ItemCategoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _uow.ItemCategories.GetByIdAsync(id, ct);
        return category is null || category.IsDeleted ? null : ToDto(category);
    }

    public async Task<IReadOnlyList<DropdownItemDto>> GetDropdownAsync(CancellationToken ct = default)
    {
        var categories = await _uow.ItemCategories.GetAllActiveAsync(ct);
        return categories.Select(c => new DropdownItemDto(c.Id, c.Code, c.Name)).ToList();
    }

    public async Task<OperationResult> SaveAsync(ItemCategorySaveRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Code))
            throw new ValidationException("Category code and name are required.");

        ItemCategory? category;
        if (request.Id.HasValue)
        {
            category = await _uow.ItemCategories.GetByIdAsync(request.Id.Value, ct);
            if (category is null || category.IsDeleted)
                throw new NotFoundException("Category not found.");
            category.ModifiedOn = DateTime.UtcNow;
        }
        else
        {
            if (await _uow.ItemCategories.CodeExistsAsync(request.Code, ct: ct))
                throw new DuplicateException($"Category code '{request.Code}' already exists.");
            category = new ItemCategory();
            await _uow.ItemCategories.AddAsync(category, ct);
        }

        category.Code = request.Code.Trim();
        category.Name = request.Name.Trim();
        category.Description = request.Description.Trim();
        category.IsActive = request.IsActive;

        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(request.Id.HasValue ? AuditAction.Update : AuditAction.Create,
            nameof(ItemCategory), category.Id,
            $"{(request.Id.HasValue ? "Updated" : "Created")} category '{category.Code} - {category.Name}'.",
            ct: ct);

        return OperationResult.Ok(request.Id.HasValue ? "Category updated." : "Category created.");
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _uow.ItemCategories.GetByIdAsync(id, ct);
        if (category is null || category.IsDeleted)
            throw new NotFoundException("Category not found.");

        if (await _uow.Items.CategoryInUseAsync(id, ct))
            throw new BusinessRuleException("This category is used by one or more items and cannot be deleted. Disable it instead.");

        category.IsDeleted = true;
        category.DeletedOn = DateTime.UtcNow;
        category.DeletedBy = _currentUser.UserId;
        category.IsActive = false;
        _uow.ItemCategories.Update(category);
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.Delete, nameof(ItemCategory), id,
            $"Deleted category '{category.Code} - {category.Name}'.", ct: ct);

        return OperationResult.Ok("Category deleted.");
    }

    public async Task<string> GenerateCodeAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetCompanySettingsAsync(ct);
        var next = await _uow.Sequences.GetNextAsync("ItemCategory", s.CategoryNumberPrefix, DateTime.Today.Year, ct);
        return $"{s.CategoryNumberPrefix}/{DateTime.Today.Year}/{next:0000}";
    }

    internal static ItemCategoryDto ToDto(ItemCategory c) => new()
    {
        Id = c.Id, Code = c.Code, Name = c.Name, Description = c.Description, IsActive = c.IsActive
    };
}

public class ItemService : IItemService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly ISettingsService _settings;

    public ItemService(IUnitOfWork uow, ICurrentUserService currentUser, IAuditService audit, ISettingsService settings)
    {
        _uow = uow;
        _currentUser = currentUser;
        _audit = audit;
        _settings = settings;
    }

    public async Task<PagedResponse<ItemDto>> GetPagedAsync(ItemSearchFilter filter, CancellationToken ct = default)
    {
        var result = await _uow.Items.GetPagedAsync(filter, ct);
        var page = new PagedResult<ItemDto>
        {
            Items = result.Items.Select(ToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
        return PagedResponse<ItemDto>.From(page);
    }

    public async Task<ItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _uow.Items.GetByIdAsync(id, ct);
        if (item is null) return null;

        var serials = await _uow.SerialNumbers.GetInStockByItemAsync(id, ct);
        var dto = ToDto(item);
        dto.InStockCount = serials.Count;
        return dto;
    }

    public async Task<IReadOnlyList<DropdownItemDto>> GetDropdownAsync(CancellationToken ct = default)
    {
        var items = await _uow.Items.GetAllActiveAsync(ct);
        return items.Select(i => new DropdownItemDto(i.Id, i.Code, i.Name, $"{i.Make} {i.Model}".Trim()))
            .ToList();
    }

    public async Task<OperationResult> SaveAsync(ItemSaveRequest request, CancellationToken ct = default)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Name)) errors.Add("Item name is required.");
        if (string.IsNullOrWhiteSpace(request.Code)) errors.Add("Item code is required.");
        if (errors.Count > 0) throw new ValidationException(errors);

        Item? item;
        if (request.Id.HasValue)
        {
            item = await _uow.Items.GetByIdAsync(request.Id.Value, ct);
            if (item is null || item.IsDeleted)
                throw new NotFoundException("Item not found.");
            item.ModifiedOn = DateTime.UtcNow;
        }
        else
        {
            if (await _uow.Items.CodeExistsAsync(request.Code, ct: ct))
                throw new DuplicateException($"Item code '{request.Code}' already exists.");
            item = new Item();
            await _uow.Items.AddAsync(item, ct);
        }

        item.Code = request.Code.Trim();
        item.Name = request.Name.Trim();
        item.CategoryId = request.CategoryId;
        item.Make = request.Make.Trim();
        item.Model = request.Model.Trim();
        item.Unit = string.IsNullOrWhiteSpace(request.Unit) ? "Nos" : request.Unit.Trim();
        item.HsnCode = request.HsnCode.Trim();
        item.Description = request.Description.Trim();
        item.IsSerialTracked = request.IsSerialTracked;
        item.IsActive = request.IsActive;

        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(request.Id.HasValue ? AuditAction.Update : AuditAction.Create,
            nameof(Item), item.Id,
            $"{(request.Id.HasValue ? "Updated" : "Created")} item '{item.Code} - {item.Name}'.",
            ct: ct);

        return OperationResult.Ok(request.Id.HasValue ? "Item updated." : "Item created.");
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _uow.Items.GetByIdAsync(id, ct);
        if (item is null || item.IsDeleted)
            throw new NotFoundException("Item not found.");

        item.IsDeleted = true;
        item.DeletedOn = DateTime.UtcNow;
        item.DeletedBy = _currentUser.UserId;
        item.IsActive = false;
        _uow.Items.Update(item);
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.Delete, nameof(Item), id,
            $"Deleted item '{item.Code} - {item.Name}'.", ct: ct);

        return OperationResult.Ok("Item deleted.");
    }

    public async Task<string> GenerateCodeAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetCompanySettingsAsync(ct);
        var next = await _uow.Sequences.GetNextAsync("Item", s.ItemNumberPrefix, DateTime.Today.Year, ct);
        return $"{s.ItemNumberPrefix}/{DateTime.Today.Year}/{next:0000}";
    }

    internal static ItemDto ToDto(Item i) => new()
    {
        Id = i.Id,
        Code = i.Code,
        Name = i.Name,
        CategoryId = i.CategoryId,
        CategoryName = i.Category?.Name ?? string.Empty,
        Make = i.Make,
        Model = i.Model,
        Unit = i.Unit,
        HsnCode = i.HsnCode,
        Description = i.Description,
        IsSerialTracked = i.IsSerialTracked,
        IsActive = i.IsActive
    };
}
