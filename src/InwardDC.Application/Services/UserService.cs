using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Exceptions;
using InwardDC.Domain.Interfaces;

namespace InwardDC.Application.Services;

/// <summary>
/// Admin-only user administration. Every method enforces that the current caller is
/// an Admin, and guards against disabling the last active admin.
/// </summary>
public class UserService : IUserService
{
    private const string DefaultPassword = "Admin@123";

    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public UserService(IUnitOfWork uow, ICurrentUserService currentUser, IAuditService audit)
    {
        _uow = uow;
        _currentUser = currentUser;
        _audit = audit;
    }

    private void EnsureAdmin()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.IsAdmin)
            throw new AuthorizationException("Only an administrator can manage users.");
    }

    public async Task<PagedResponse<UserDto>> GetPagedAsync(Domain.Criteria.UserSearchFilter filter, CancellationToken ct = default)
    {
        EnsureAdmin();
        var result = await _uow.Users.GetPagedAsync(filter, ct);
        var page = new Domain.Criteria.PagedResult<UserDto>
        {
            Items = result.Items.Select(ToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
        return PagedResponse<UserDto>.From(page);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(id, ct);
        return user is null ? null : ToDto(user);
    }

    public async Task<OperationResult> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();

        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.FullName))
            throw new ValidationException("User name and full name are required.");

        if (request.Password != request.ConfirmPassword)
            throw new ValidationException("Password and confirmation do not match.");

        if (!PasswordHasher.IsStrong(request.Password))
            throw new ValidationException("Password must be at least 6 characters and contain letters and digits.");

        var userName = request.UserName.Trim();
        if (await _uow.Users.UserNameExistsAsync(userName, ct: ct))
            throw new DuplicateException($"User name '{userName}' already exists.");

        var (hash, salt) = PasswordHasher.Hash(request.Password);
        var user = new User
        {
            UserName = userName,
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            Role = request.Role,
            IsActive = true,
            MustChangePassword = request.MustChangePassword,
            PasswordHash = hash,
            PasswordSalt = salt
        };

        await _uow.Users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.Create, nameof(User), user.Id,
            $"Created user '{user.UserName}' with role {user.Role}.", ct: ct);

        return OperationResult.Ok($"User '{user.UserName}' created successfully.");
    }

    public async Task<OperationResult> UpdateAsync(UpdateUserRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();

        var user = await _uow.Users.GetByIdAsync(request.Id, ct);
        if (user is null || user.IsDeleted)
            throw new NotFoundException("User not found.");

        user.FullName = request.FullName.Trim();
        user.Email = request.Email.Trim();
        user.Phone = request.Phone.Trim();
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.Update, nameof(User), user.Id,
            $"Updated user '{user.UserName}'.", ct: ct);

        return OperationResult.Ok($"User '{user.UserName}' updated.");
    }

    public async Task<OperationResult> DisableAsync(Guid id, CancellationToken ct = default)
    {
        EnsureAdmin();
        var user = await _uow.Users.GetByIdAsync(id, ct);
        if (user is null || user.IsDeleted)
            throw new NotFoundException("User not found.");

        if (user.Id == _currentUser.UserId)
            throw new BusinessRuleException("You cannot disable your own account.");

        if (user.Role == UserRole.Admin && !await HasAnotherActiveAdminAsync(user.Id, ct))
            throw new BusinessRuleException("Cannot disable the last active administrator.");

        user.IsActive = false;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.Disable, nameof(User), user.Id,
            $"Disabled user '{user.UserName}'.", ct: ct);

        return OperationResult.Ok($"User '{user.UserName}' disabled.");
    }

    public async Task<OperationResult> EnableAsync(Guid id, CancellationToken ct = default)
    {
        EnsureAdmin();
        var user = await _uow.Users.GetByIdAsync(id, ct);
        if (user is null || user.IsDeleted)
            throw new NotFoundException("User not found.");

        user.IsActive = true;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.Enable, nameof(User), user.Id,
            $"Enabled user '{user.UserName}'.", ct: ct);

        return OperationResult.Ok($"User '{user.UserName}' enabled.");
    }

    public async Task<OperationResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        EnsureAdmin();

        if (request.NewPassword != request.ConfirmPassword)
            throw new ValidationException("Password and confirmation do not match.");

        var newPassword = string.IsNullOrWhiteSpace(request.NewPassword) ? DefaultPassword : request.NewPassword;

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            // Fall through with the default password; no strength check needed on defaults.
        }
        else if (!PasswordHasher.IsStrong(newPassword))
        {
            throw new ValidationException("Password must be at least 6 characters and contain letters and digits.");
        }

        var user = await _uow.Users.GetByIdAsync(request.UserId, ct);
        if (user is null || user.IsDeleted)
            throw new NotFoundException("User not found.");

        var (hash, salt) = PasswordHasher.Hash(newPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.MustChangePassword = true;
        user.ModifiedOn = DateTime.UtcNow;

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.ResetPassword, nameof(User), user.Id,
            $"Password reset for user '{user.UserName}'.", ct: ct);

        return OperationResult.Ok($"Password for '{user.UserName}' has been reset.");
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        EnsureAdmin();

        var user = await _uow.Users.GetByIdAsync(id, ct);
        if (user is null || user.IsDeleted)
            throw new NotFoundException("User not found.");

        if (user.Id == _currentUser.UserId)
            throw new BusinessRuleException("You cannot delete your own account.");

        if (user.Role == UserRole.Admin && !await HasAnotherActiveAdminAsync(user.Id, ct))
            throw new BusinessRuleException("Cannot delete the last active administrator.");

        user.IsDeleted = true;
        user.DeletedOn = DateTime.UtcNow;
        user.DeletedBy = _currentUser.UserId;
        user.IsActive = false;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.Delete, nameof(User), user.Id,
            $"Deleted user '{user.UserName}'.", ct: ct);

        return OperationResult.Ok($"User '{user.UserName}' deleted.");
    }

    private async Task<bool> HasAnotherActiveAdminAsync(Guid exceptId, CancellationToken ct)
    {
        var users = await _uow.Users.GetAllAsync(ct);
        return users.Any(u => u.Role == UserRole.Admin && u.IsActive && u.Id != exceptId);
    }

    internal static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        UserName = u.UserName,
        FullName = u.FullName,
        Email = u.Email,
        Phone = u.Phone,
        Role = u.Role,
        IsActive = u.IsActive,
        MustChangePassword = u.MustChangePassword,
        LastLoginOn = u.LastLoginOn,
        CreatedOn = u.CreatedOn
    };
}
