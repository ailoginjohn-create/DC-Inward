using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;

namespace InwardDC.Domain.Interfaces;

/// <summary>
/// User data access contract. The presentation/business layers only see this
/// interface, so the underlying store (EF Core + SQLite today, a REST API tomorrow)
/// can be swapped without touching the UI or business logic.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByUserNameAsync(string userName, CancellationToken ct = default);
    Task<PagedResult<User>> GetPagedAsync(UserSearchFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task<bool> UserNameExistsAsync(string userName, Guid? exceptId = null, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    void Update(User user);
    Task<int> CountAsync(CancellationToken ct = default);
}
