using Xunit;
using InwardDC.Application.DTOs;
using InwardDC.Application.Services;
using InwardDC.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace InwardDC.Tests;

public class UserModuleRestrictionTests
{
    [Fact]
    public async Task CreateUser_WithRestrictedModules_PersistsAndEnforcesAccess()
    {
        using var app = new TestApp();
        var users = new UserService(app.Uow, app.CurrentUser, new AuditService(app.Uow, app.CurrentUser));
        var auth = new AuthService(app.Uow, app.CurrentUser,
            new AuditService(app.Uow, app.CurrentUser), NullLogger<AuthService>.Instance);

        var create = await users.CreateAsync(new CreateUserRequest
        {
            UserName = "operator",
            FullName = "Test Operator",
            Role = UserRole.User,
            Password = "OpPass123",
            ConfirmPassword = "OpPass123",
            AllowedModules = new[] { "inward", "customers" }
        });
        Assert.True(create.Success);

        var stored = await app.Uow.Users.GetByUserNameAsync("operator");
        Assert.NotNull(stored!.AllowedModules);
        Assert.Equal(new[] { "customers", "inward" }, stored.AllowedModules!.OrderBy(x => x));

        var login = await auth.LoginAsync(new LoginRequest { UserName = "operator", Password = "OpPass123" });
        Assert.True(login.Success);

        Assert.True(app.CurrentUser.CanAccessModule("inward"));
        Assert.True(app.CurrentUser.CanAccessModule("customers"));
        Assert.False(app.CurrentUser.CanAccessModule("reports"));
        Assert.False(app.CurrentUser.CanAccessModule("audit"));
        Assert.True(app.CurrentUser.CanAccessModule("dashboard"));
    }

    [Fact]
    public async Task CreateUser_WithoutModules_IsUnrestricted()
    {
        using var app = new TestApp();
        var users = new UserService(app.Uow, app.CurrentUser, new AuditService(app.Uow, app.CurrentUser));
        var auth = new AuthService(app.Uow, app.CurrentUser,
            new AuditService(app.Uow, app.CurrentUser), NullLogger<AuthService>.Instance);

        var create = await users.CreateAsync(new CreateUserRequest
        {
            UserName = "operator",
            FullName = "Test Operator",
            Role = UserRole.User,
            Password = "OpPass123",
            ConfirmPassword = "OpPass123"
        });
        Assert.True(create.Success);

        var stored = await app.Uow.Users.GetByUserNameAsync("operator");
        Assert.Null(stored!.AllowedModules);

        var login = await auth.LoginAsync(new LoginRequest { UserName = "operator", Password = "OpPass123" });
        Assert.True(login.Success);
        Assert.True(app.CurrentUser.CanAccessModule("reports"));
        Assert.True(app.CurrentUser.CanAccessModule("audit"));
    }

    [Fact]
    public async Task AdminUsers_AreNeverRestricted()
    {
        using var app = new TestApp();
        var users = new UserService(app.Uow, app.CurrentUser, new AuditService(app.Uow, app.CurrentUser));

        var create = await users.CreateAsync(new CreateUserRequest
        {
            UserName = "coadmin",
            FullName = "Co Admin",
            Role = UserRole.Admin,
            Password = "CoAdmin123",
            ConfirmPassword = "CoAdmin123",
            AllowedModules = new[] { "inward" }
        });
        Assert.True(create.Success);

        var stored = await app.Uow.Users.GetByUserNameAsync("coadmin");
        Assert.Null(stored!.AllowedModules);
    }

    [Fact]
    public async Task CreateUser_DropsUnknownModuleKeys()
    {
        using var app = new TestApp();
        var users = new UserService(app.Uow, app.CurrentUser, new AuditService(app.Uow, app.CurrentUser));

        var create = await users.CreateAsync(new CreateUserRequest
        {
            UserName = "operator",
            FullName = "Test Operator",
            Role = UserRole.User,
            Password = "OpPass123",
            ConfirmPassword = "OpPass123",
            AllowedModules = new[] { "inward", "bogus-module" }
        });
        Assert.True(create.Success);

        var stored = await app.Uow.Users.GetByUserNameAsync("operator");
        Assert.Equal(new[] { "inward" }, stored!.AllowedModules!.ToArray());
    }

    [Fact]
    public async Task UpdateUser_ChangesAllowedModules()
    {
        using var app = new TestApp();
        var users = new UserService(app.Uow, app.CurrentUser, new AuditService(app.Uow, app.CurrentUser));

        var create = await users.CreateAsync(new CreateUserRequest
        {
            UserName = "operator",
            FullName = "Test Operator",
            Role = UserRole.User,
            Password = "OpPass123",
            ConfirmPassword = "OpPass123",
            AllowedModules = new[] { "inward" }
        });
        Assert.True(create.Success);

        var user = await app.Uow.Users.GetByUserNameAsync("operator");

        var update = await users.UpdateAsync(new UpdateUserRequest
        {
            Id = user!.Id,
            FullName = "Test Operator",
            Email = "op@test.com",
            Phone = string.Empty,
            Role = UserRole.User,
            IsActive = true,
            AllowedModules = new[] { "reports", "audit" }
        });
        Assert.True(update.Success);

        var after = await app.Uow.Users.GetByUserNameAsync("operator");
        Assert.Equal(new[] { "audit", "reports" }, after!.AllowedModules!.OrderBy(x => x));
    }
}
