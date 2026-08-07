using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Interfaces;
using InwardDC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InwardDC.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default)
        => _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName.ToLower() == userName.ToLower(), ct);

    public async Task<PagedResult<User>> GetPagedAsync(UserSearchFilter filter, CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking().Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(u => u.UserName.Contains(term)
                || u.FullName.Contains(term)
                || u.Email.Contains(term)
                || u.Phone.Contains(term));
        }

        if (filter.Role.HasValue)
            query = query.Where(u => u.Role == filter.Role.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(u => u.IsActive == filter.IsActive.Value);

        query = ApplySorting(query, filter);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(ct);

        return new PagedResult<User> { Items = items, TotalCount = total, Page = filter.Page, PageSize = filter.PageSize };
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
        => await _db.Users.AsNoTracking().Where(u => !u.IsDeleted).OrderBy(u => u.FullName).ToListAsync(ct);

    public Task<bool> UserNameExistsAsync(string userName, Guid? exceptId = null, CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking().Where(u => !u.IsDeleted && u.UserName.ToLower() == userName.ToLower());
        if (exceptId.HasValue)
            query = query.Where(u => u.Id != exceptId.Value);
        return query.AnyAsync(ct);
    }

    public Task AddAsync(User user, CancellationToken ct = default)
        => _db.Users.AddAsync(user, ct).AsTask();

    public void Update(User user) => _db.UpdateTracked(user);

    public Task<int> CountAsync(CancellationToken ct = default)
        => _db.Users.AsNoTracking().CountAsync(u => !u.IsDeleted, ct);

    private static IQueryable<User> ApplySorting(IQueryable<User> query, PagedRequest filter)
    {
        var desc = filter.SortDescending;
        return (filter.SortBy?.ToLower()) switch
        {
            "username" => desc ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName),
            "fullname" => desc ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
            "role" => desc ? query.OrderByDescending(u => u.Role) : query.OrderBy(u => u.Role),
            "createdon" => desc ? query.OrderByDescending(u => u.CreatedOn) : query.OrderBy(u => u.CreatedOn),
            _ => desc ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName)
        };
    }
}
