using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Exceptions;
using InwardDC.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace InwardDC.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUnitOfWork uow, ICurrentUserService currentUser, IAuditService audit, ILogger<AuthService> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            return new LoginResult { Success = false, Message = "User name and password are required." };

        var user = await _uow.Users.GetByUserNameAsync(request.UserName.Trim(), ct);
        if (user is null || user.IsDeleted)
            return new LoginResult { Success = false, Message = "Invalid user name or password." };

        if (!user.IsActive)
            return new LoginResult { Success = false, Message = "This user account has been disabled. Contact your administrator." };

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            await _audit.AddAsync(AuditAction.Login, nameof(User), user.Id,
                $"Failed login attempt for user '{user.UserName}'.", ct: ct);
            return new LoginResult { Success = false, Message = "Invalid user name or password." };
        }

        user.LastLoginOn = DateTime.UtcNow;
        user.ModifiedOn = DateTime.UtcNow;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);

        var dto = ToDto(user);
        _currentUser.SignIn(dto);

        await _audit.AddAsync(AuditAction.Login, nameof(User), user.Id,
            $"User '{user.UserName}' logged in successfully.", ct: ct);

        return new LoginResult
        {
            Success = true,
            User = dto,
            MustChangePassword = user.MustChangePassword,
            Message = "Login successful."
        };
    }

    public async Task<OperationResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new ValidationException("New password and confirmation do not match.");

        if (!PasswordHasher.IsStrong(request.NewPassword))
            throw new ValidationException("Password must be at least 6 characters and contain letters and digits.");

        var user = await _uow.Users.GetByIdAsync(request.UserId, ct);
        if (user is null || user.IsDeleted)
            throw new NotFoundException("User not found.");

        if (!PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            throw new AuthenticationException("Current password is incorrect.");

        var (hash, salt) = PasswordHasher.Hash(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.MustChangePassword = false;
        user.ModifiedOn = DateTime.UtcNow;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.ChangePassword, nameof(User), user.Id,
            $"User '{user.UserName}' changed their password.", ct: ct);

        return OperationResult.Ok("Password changed successfully.");
    }

    private static UserDto ToDto(User u) => new()
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
