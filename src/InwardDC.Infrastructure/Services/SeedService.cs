using InwardDC.Application.Common;
using InwardDC.Application.Interfaces;
using InwardDC.Application.Services;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace InwardDC.Infrastructure.Services;

/// <summary>
/// Idempotent first-run seeding: creates the default admin account and the standard
/// configuration settings (nothing is hardcoded at runtime afterwards).
/// </summary>
public class SeedService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SeedService> _logger;

    public SeedService(IUnitOfWork uow, ILogger<SeedService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedAdminUserAsync(ct);
        await SeedDefaultSettingsAsync(ct);
        await SeedDefaultCategoriesAsync(ct);
        await SeedDefaultPurposesAsync(ct);
        await _uow.SaveChangesAsync(ct);
    }

    private async Task SeedAdminUserAsync(CancellationToken ct)
    {
        if (await _uow.Users.GetByUserNameAsync("admin", ct) is not null)
            return;

        var (hash, salt) = PasswordHasher.Hash("Admin@123");
        await _uow.Users.AddAsync(new User
        {
            UserName = "admin",
            FullName = "System Administrator",
            Email = "admin@local",
            Role = UserRole.Admin,
            IsActive = true,
            MustChangePassword = true,
            PasswordHash = hash,
            PasswordSalt = salt
        }, ct);

        _logger.LogInformation("Default admin user seeded.");
    }

    private async Task SeedDefaultSettingsAsync(CancellationToken ct)
    {
        if (await _uow.Settings.KeyExistsAsync(SettingsService.Keys.CompanyName, ct))
            return;

        var defaults = new (string Key, string Value, string Group, string Description)[]
        {
            (SettingsService.Keys.CompanyName, "My Company", "Company", "Company / business name shown on documents"),
            (SettingsService.Keys.CompanyAddressLine1, "", "Company", "Address line 1"),
            (SettingsService.Keys.CompanyAddressLine2, "", "Company", "Address line 2"),
            (SettingsService.Keys.CompanyCity, "", "Company", "City"),
            (SettingsService.Keys.CompanyState, "", "Company", "State"),
            (SettingsService.Keys.CompanyPincode, "", "Company", "PIN code"),
            (SettingsService.Keys.CompanyPhone, "", "Company", "Phone"),
            (SettingsService.Keys.CompanyEmail, "", "Company", "Email"),
            (SettingsService.Keys.CompanyGstin, "", "Company", "GSTIN"),
            (SettingsService.Keys.CompanyPan, "", "Company", "PAN"),
            (SettingsService.Keys.CompanyLogoPath, "", "Company", "Path to the company logo image"),
            (SettingsService.Keys.InwardNumberPrefix, "INW", "Numbering", "Prefix for auto inward numbers"),
            (SettingsService.Keys.DcNumberPrefix, "DC", "Numbering", "Prefix for auto DC numbers"),
            (SettingsService.Keys.CustomerNumberPrefix, "CUS", "Numbering", "Prefix for auto customer codes"),
            (SettingsService.Keys.VendorNumberPrefix, "VEN", "Numbering", "Prefix for auto vendor codes"),
            (SettingsService.Keys.ItemNumberPrefix, "ITM", "Numbering", "Prefix for auto item codes"),
            (SettingsService.Keys.CategoryNumberPrefix, "CAT", "Numbering", "Prefix for auto category codes"),
            (SettingsService.Keys.FooterNote, "Subject to our terms & conditions.", "Documents", "Footer note on printed documents"),
            (SettingsService.Keys.RequireSerialForTrackedItems, "true", "Documents", "Require a serial number for serial tracked items")
        };

        foreach (var (key, value, group, description) in defaults)
        {
            await _uow.Settings.AddAsync(new Setting
            {
                Key = key,
                Value = value,
                Group = group,
                Description = description,
                DataType = "string",
                IsSystem = true
            }, ct);
        }
    }

    private async Task SeedDefaultCategoriesAsync(CancellationToken ct)
    {
        if (await _uow.ItemCategories.GetByCodeAsync("EQUIP", ct) is not null)
            return;

        await _uow.ItemCategories.AddAsync(new ItemCategory
        {
            Code = "EQUIP",
            Name = "Biomedical Equipment",
            Description = "Medical equipment and devices",
            IsActive = true
        }, ct);
        await _uow.ItemCategories.AddAsync(new ItemCategory
        {
            Code = "CONS",
            Name = "Consumables",
            Description = "Single use and consumable supplies",
            IsActive = true
        }, ct);
        await _uow.ItemCategories.AddAsync(new ItemCategory
        {
            Code = "SPARE",
            Name = "Spare Parts",
            Description = "Spare parts and components",
            IsActive = true
        }, ct);
    }

    private async Task SeedDefaultPurposesAsync(CancellationToken ct)
    {
        if (await _uow.Purposes.GetByNameAsync("Evaluation", ct) is not null)
            return;

        var defaults = new[]
        {
            ("Evaluation", "For evaluation / trial use"),
            ("Testing", "For testing and calibration"),
            ("Demo", "For demonstration to customers"),
            ("Service", "For repair and service work"),
            ("Other", "Other purpose")
        };

        foreach (var (name, description) in defaults)
        {
            await _uow.Purposes.AddAsync(new Purpose
            {
                Name = name,
                Description = description,
                IsActive = true
            }, ct);
        }
    }
}
