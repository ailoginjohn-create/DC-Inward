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
    void SignIn(DTOs.UserDto user);
    void SignOut();
}
