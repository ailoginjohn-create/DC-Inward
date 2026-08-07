using InwardDC.Domain.Criteria;
using Microsoft.EntityFrameworkCore;

namespace InwardDC.Infrastructure.Repositories;

/// <summary>Shared pagination helper for repository implementations.</summary>
internal static class PagingHelper
{
    public static async Task<PagedResult<T>> ToPagedAsync<T>(
        IQueryable<T> query, PagedRequest request, CancellationToken ct = default)
    {
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
