using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Exceptions;
using InwardDC.Domain.Interfaces;

namespace InwardDC.Application.Services;

/// <summary>
/// Purpose master data (evaluation / testing / demo / ...). Purposes are simple named
/// labels used by inward entries and dispatch challans; the list is fully editable.
/// </summary>
public class PurposeService : IPurposeService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public PurposeService(IUnitOfWork uow, ICurrentUserService currentUser, IAuditService audit)
    {
        _uow = uow;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PagedResponse<PurposeDto>> GetPagedAsync(PurposeSearchFilter filter, CancellationToken ct = default)
    {
        var result = await _uow.Purposes.GetPagedAsync(filter, ct);
        var page = new PagedResult<PurposeDto>
        {
            Items = result.Items.Select(ToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
        return PagedResponse<PurposeDto>.From(page);
    }

    public async Task<PurposeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var purpose = await _uow.Purposes.GetByIdAsync(id, ct);
        return purpose is null || purpose.IsDeleted ? null : ToDto(purpose);
    }

    public async Task<IReadOnlyList<DropdownItemDto>> GetDropdownAsync(CancellationToken ct = default)
    {
        var purposes = await _uow.Purposes.GetAllActiveAsync(ct);
        return purposes.Select(p => new DropdownItemDto(p.Id, p.Name, p.Name, p.Description)).ToList();
    }

    public async Task<OperationResult> SaveAsync(PurposeSaveRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Purpose name is required.");

        Purpose? purpose;
        if (request.Id.HasValue)
        {
            purpose = await _uow.Purposes.GetByIdAsync(request.Id.Value, ct);
            if (purpose is null || purpose.IsDeleted)
                throw new NotFoundException("Purpose not found.");
            purpose.ModifiedOn = DateTime.UtcNow;
        }
        else
        {
            if (await _uow.Purposes.NameExistsAsync(request.Name.Trim(), exceptId: request.Id, ct: ct))
                throw new DuplicateException($"Purpose '{request.Name.Trim()}' already exists.");
            purpose = new Purpose();
            await _uow.Purposes.AddAsync(purpose, ct);
        }

        purpose.Name = request.Name.Trim();
        purpose.Description = request.Description.Trim();
        purpose.IsActive = request.IsActive;

        if (request.Id.HasValue)
            _uow.Purposes.Update(purpose);

        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(request.Id.HasValue ? AuditAction.Update : AuditAction.Create,
            nameof(Purpose), purpose.Id,
            $"{(request.Id.HasValue ? "Updated" : "Created")} purpose '{purpose.Name}'.",
            ct: ct);

        return OperationResult.Ok(request.Id.HasValue ? "Purpose updated." : "Purpose created.");
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var purpose = await _uow.Purposes.GetByIdAsync(id, ct);
        if (purpose is null || purpose.IsDeleted)
            throw new NotFoundException("Purpose not found.");

        if (await _uow.Purposes.IsInUseAsync(id, ct))
            throw new BusinessRuleException("This purpose is used by one or more inwards or dispatch challans and cannot be deleted. Disable it instead.");

        purpose.IsDeleted = true;
        purpose.DeletedOn = DateTime.UtcNow;
        purpose.DeletedBy = _currentUser.UserId;
        purpose.IsActive = false;
        _uow.Purposes.Update(purpose);
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.Delete, nameof(Purpose), id,
            $"Deleted purpose '{purpose.Name}'.", ct: ct);

        return OperationResult.Ok("Purpose deleted.");
    }

    private static PurposeDto ToDto(Purpose p) => new()
    {
        Id = p.Id, Name = p.Name, Description = p.Description, IsActive = p.IsActive
    };
}
