using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Domain.Catalog;

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
    public IReadOnlyCollection<string>? AllowedModules { get; private set; }

    public bool CanAccessModule(string moduleKey)
    {
        if (IsAdmin)
            return true;

        if (string.Equals(moduleKey, AppModule.Dashboard.Key, StringComparison.OrdinalIgnoreCase))
            return true;

        return AllowedModules is null
            || AllowedModules.Contains(moduleKey, StringComparer.OrdinalIgnoreCase);
    }

    public void SignIn(UserDto user)
    {
        UserId = user.Id;
        UserName = user.UserName;
        FullName = user.FullName;
        IsAdmin = user.IsAdmin;
        AllowedModules = user.AllowedModules;
    }

    public void SignOut()
    {
        UserId = null;
        UserName = string.Empty;
        FullName = string.Empty;
        IsAdmin = false;
        AllowedModules = null;
    }
}
