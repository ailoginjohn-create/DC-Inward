using Xunit;
using InwardDC.Application.DTOs;
using InwardDC.Application.Services;
using InwardDC.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace InwardDC.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_Succeeds_WithSeededAdminCredentials()
    {
        using var app = new TestApp();
        var service = new AuthService(app.Uow, app.CurrentUser,
            new AuditService(app.Uow, app.CurrentUser), NullLogger<AuthService>.Instance);

        var result = await service.LoginAsync(new LoginRequest { UserName = "admin", Password = "Admin@123" });

        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.Equal("admin", result.User!.UserName);
        Assert.True(result.User.IsAdmin);
        Assert.True(result.MustChangePassword);
        Assert.True(app.CurrentUser.IsAuthenticated);
        Assert.Equal(result.User.Id, app.CurrentUser.UserId);
    }

    [Fact]
    public async Task LoginAsync_Fails_WithWrongPassword()
    {
        using var app = new TestApp();
        var service = new AuthService(app.Uow, app.CurrentUser,
            new AuditService(app.Uow, app.CurrentUser), NullLogger<AuthService>.Instance);

        var result = await service.LoginAsync(new LoginRequest { UserName = "admin", Password = "WrongPass123" });

        Assert.False(result.Success);
        Assert.Null(result.User);
        // A failed login must not disturb the existing session (still admin).
        Assert.True(app.CurrentUser.IsAuthenticated);
        Assert.Equal("admin", app.CurrentUser.UserName);
    }

    [Fact]
    public async Task ChangePasswordAsync_ThenLoginWithNewPassword()
    {
        using var app = new TestApp();
        var service = new AuthService(app.Uow, app.CurrentUser,
            new AuditService(app.Uow, app.CurrentUser), NullLogger<AuthService>.Instance);

        var admin = await app.Uow.Users.GetByUserNameAsync("admin");

        var change = await service.ChangePasswordAsync(new ChangePasswordRequest
        {
            UserId = admin!.Id,
            CurrentPassword = "Admin@123",
            NewPassword = "NewPass456",
            ConfirmPassword = "NewPass456"
        });

        Assert.True(change.Success);

        var wrongOld = await service.LoginAsync(new LoginRequest { UserName = "admin", Password = "Admin@123" });
        Assert.False(wrongOld.Success);

        var rightNew = await service.LoginAsync(new LoginRequest { UserName = "admin", Password = "NewPass456" });
        Assert.True(rightNew.Success);
        Assert.False(rightNew.MustChangePassword);
    }

    [Fact]
    public async Task ChangePasswordAsync_Throws_OnWrongCurrentPassword()
    {
        using var app = new TestApp();
        var service = new AuthService(app.Uow, app.CurrentUser,
            new AuditService(app.Uow, app.CurrentUser), NullLogger<AuthService>.Instance);

        var admin = await app.Uow.Users.GetByUserNameAsync("admin");

        await Assert.ThrowsAsync<AuthenticationException>(() => service.ChangePasswordAsync(
            new ChangePasswordRequest
            {
                UserId = admin!.Id,
                CurrentPassword = "not-the-current",
                NewPassword = "NewPass456",
                ConfirmPassword = "NewPass456"
            }));
    }
}
