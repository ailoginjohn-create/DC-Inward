using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Domain.Enums;
using InwardDC.Infrastructure.Data;
using InwardDC.Infrastructure.Repositories;
using InwardDC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InwardDC.Tests;

/// <summary>Test double for the session-bound current user.</summary>
public sealed class TestCurrentUser : ICurrentUserService
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

        if (string.Equals(moduleKey, "dashboard", StringComparison.OrdinalIgnoreCase))
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

/// <summary>
/// A real SQLite-backed application context for integration-style tests: applies the
/// migrations, seeds the admin user + default settings/categories, and signs in as
/// admin so every service can be exercised end to end.
/// </summary>
public sealed class TestApp : IDisposable
{
    public string RootDir { get; }
    public string DbFile { get; }
    public AppDbContext Db { get; }
    public UnitOfWork Uow { get; }
    public TestCurrentUser CurrentUser { get; } = new();

    public TestApp()
    {
        RootDir = Path.Combine(Path.GetTempPath(), "inwarddc-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootDir);
        DbFile = Path.Combine(RootDir, "test.db");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={DbFile}",
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name!))
            .Options;

        Db = new AppDbContext(options);
        Db.Database.Migrate();

        Uow = new UnitOfWork(Db);

        var seeder = new SeedService(Uow, NullLogger<SeedService>.Instance);
        seeder.SeedAsync().GetAwaiter().GetResult();

        // The seeder leaves its entities tracked; drop them so later Update() calls
        // (e.g. login touching LastLoginOn) never collide with a tracked duplicate.
        Db.ChangeTracker.Clear();

        var admin = Uow.Users.GetByUserNameAsync("admin").GetAwaiter().GetResult();
        if (admin is not null)
        {
            CurrentUser.SignIn(new UserDto
            {
                Id = admin.Id,
                UserName = admin.UserName,
                FullName = admin.FullName,
                Role = admin.Role
            });
        }
    }

    /// <summary>Adds and commits a serial-tracked item master (e.g. biomedical equipment).</summary>
    public async Task<Guid> AddSerialTrackedItemAsync(string code, string name, CancellationToken ct = default)
    {
        var item = new Domain.Entities.Item
        {
            Code = code,
            Name = name,
            Unit = "Nos",
            IsSerialTracked = true,
            IsActive = true
        };
        await Uow.Items.AddAsync(item, ct);
        await Uow.SaveChangesAsync(ct);
        return item.Id;
    }

    /// <summary>Adds and commits a non-serial-tracked item master.</summary>
    public async Task<Guid> AddPlainItemAsync(string code, string name, CancellationToken ct = default)
    {
        var item = new Domain.Entities.Item
        {
            Code = code,
            Name = name,
            Unit = "Nos",
            IsSerialTracked = false,
            IsActive = true
        };
        await Uow.Items.AddAsync(item, ct);
        await Uow.SaveChangesAsync(ct);
        return item.Id;
    }

    /// <summary>Adds and commits a customer master.</summary>
    public async Task<Guid> AddCustomerAsync(string name, CancellationToken ct = default)
    {
        var customer = new Domain.Entities.Customer
        {
            Code = $"CUS-{Guid.NewGuid():N}"[..12],
            Name = name,
            IsActive = true
        };
        await Uow.Customers.AddAsync(customer, ct);
        await Uow.SaveChangesAsync(ct);
        return customer.Id;
    }

    public void Dispose()
    {
        Db.Dispose();
        try
        {
            if (Directory.Exists(RootDir))
                Directory.Delete(RootDir, recursive: true);
        }
        catch (IOException)
        {
            // Test cleanup best effort only.
        }
    }
}
