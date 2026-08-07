using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Interfaces;

namespace InwardDC.Application.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public AuditService(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task AddAsync(AuditAction action, string entityType, Guid? entityId, string description,
        string details = "", CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            UserId = _currentUser.UserId,
            UserName = _currentUser.UserName,
            FullName = _currentUser.FullName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            Details = details,
            IpAddress = Environment.MachineName,
            Timestamp = DateTime.UtcNow
        };

        await _uow.AuditLogs.AddAsync(log, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<PagedResponse<AuditLogDto>> GetPagedAsync(AuditLogFilter filter, CancellationToken ct = default)
    {
        var result = await _uow.AuditLogs.GetPagedAsync(filter, ct);
        var page = new PagedResult<AuditLogDto>
        {
            Items = result.Items.Select(ToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
        return PagedResponse<AuditLogDto>.From(page);
    }

    internal static AuditLogDto ToDto(AuditLog x) => new()
    {
        Id = x.Id,
        UserId = x.UserId,
        UserName = x.UserName,
        FullName = x.FullName,
        Action = x.Action,
        EntityType = x.EntityType,
        EntityId = x.EntityId,
        Description = x.Description,
        Details = x.Details,
        IpAddress = x.IpAddress,
        Timestamp = x.Timestamp
    };
}
