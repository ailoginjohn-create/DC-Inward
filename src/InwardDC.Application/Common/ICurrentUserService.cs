namespace InwardDC.Application.Common;

/// <summary>
/// Abstraction over "who is using the app right now". Implemented by the WPF shell
/// (session) so that services never depend on UI specifics. In a future web/mobile
/// client, this is satisfied by the authentication middleware instead.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string UserName { get; }
    string FullName { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }

    /// <summary>
    /// Module keys the signed-in user may access, or null when unrestricted
    /// (all modules). Administrators are always unrestricted.
    /// </summary>
    IReadOnlyCollection<string>? AllowedModules { get; }

    /// <summary>
    /// Whether the current user may open the module identified by <paramref name="moduleKey"/>.
    /// Administrators and the Dashboard are always allowed.
    /// </summary>
    bool CanAccessModule(string moduleKey);

    void SignIn(DTOs.UserDto user);
    void SignOut();
}
