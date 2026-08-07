using InwardDC.Application.Common;
using InwardDC.Application.DTOs;

namespace InwardDC.App.Services;

/// <summary>
/// Session-bound current user. The WPF shell signs the user in/out; every service
/// reads identity from here instead of the UI layer.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public bool IsAuthenticated => UserId.HasValue;
    public bool IsAdmin { get; private set; }

    public void SignIn(UserDto user)
    {
        UserId = user.Id;
        UserName = user.UserName;
        FullName = user.FullName;
        IsAdmin = user.IsAdmin;
    }

    public void SignOut()
    {
        UserId = null;
        UserName = string.Empty;
        FullName = string.Empty;
        IsAdmin = false;
    }
}
