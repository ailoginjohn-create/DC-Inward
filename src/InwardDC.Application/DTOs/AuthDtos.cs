using InwardDC.Domain.Enums;

namespace InwardDC.Application.DTOs;

public class LoginRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public Guid UserId { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public Guid UserId { get; set; }
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public UserDto? User { get; set; }
    public bool MustChangePassword { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? LastLoginOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool IsAdmin => Role == UserRole.Admin;
}

public class CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; } = true;
}

public class UpdateUserRequest
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}
