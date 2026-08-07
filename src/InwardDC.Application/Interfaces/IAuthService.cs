using InwardDC.Application.DTOs;
using InwardDC.Domain.Criteria;

namespace InwardDC.Application.Interfaces;

/// <summary>Authentication and password management contract.</summary>
public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<OperationResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
}

/// <summary>Admin user management (create, edit, disable, enable, reset password).</summary>
public interface IUserService
{
    Task<PagedResponse<UserDto>> GetPagedAsync(UserSearchFilter filter, CancellationToken ct = default);
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(UpdateUserRequest request, CancellationToken ct = default);
    Task<OperationResult> DisableAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> EnableAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
