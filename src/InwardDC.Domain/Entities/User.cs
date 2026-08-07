using InwardDC.Domain.Enums;

namespace InwardDC.Domain.Entities;

/// <summary>Application user with role-based access and PBKDF2 password hashing.</summary>
public class User : EntityBase
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTime? LastLoginOn { get; set; }
    public string LastLoginIp { get; set; } = string.Empty;
}
