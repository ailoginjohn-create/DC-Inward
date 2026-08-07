using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Interfaces;

namespace InwardDC.Application.Services;

public class SettingsService : ISettingsService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public SettingsService(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<CompanySettingsDto> GetCompanySettingsAsync(CancellationToken ct = default)
    {
        var settings = await _uow.Settings.GetAllAsync(ct);
        var dict = settings.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

        return new CompanySettingsDto
        {
            CompanyName = Get(dict, Keys.CompanyName),
            CompanyAddressLine1 = Get(dict, Keys.CompanyAddressLine1),
            CompanyAddressLine2 = Get(dict, Keys.CompanyAddressLine2),
            CompanyCity = Get(dict, Keys.CompanyCity),
            CompanyState = Get(dict, Keys.CompanyState),
            CompanyPincode = Get(dict, Keys.CompanyPincode),
            CompanyPhone = Get(dict, Keys.CompanyPhone),
            CompanyEmail = Get(dict, Keys.CompanyEmail),
            CompanyGSTIN = Get(dict, Keys.CompanyGstin),
            CompanyPAN = Get(dict, Keys.CompanyPan),
            CompanyLogoPath = Get(dict, Keys.CompanyLogoPath),
            InwardNumberPrefix = Get(dict, Keys.InwardNumberPrefix, "INW"),
            DcNumberPrefix = Get(dict, Keys.DcNumberPrefix, "DC"),
            CustomerNumberPrefix = Get(dict, Keys.CustomerNumberPrefix, "CUS"),
            VendorNumberPrefix = Get(dict, Keys.VendorNumberPrefix, "VEN"),
            ItemNumberPrefix = Get(dict, Keys.ItemNumberPrefix, "ITM"),
            CategoryNumberPrefix = Get(dict, Keys.CategoryNumberPrefix, "CAT"),
            FooterNote = Get(dict, Keys.FooterNote),
            RequireSerialForTrackedItems = bool.TryParse(Get(dict, Keys.RequireSerialForTrackedItems, "true"), out var rs) && rs
        };
    }

    public async Task<OperationResult> SaveCompanySettingsAsync(CompanySettingsDto s, CancellationToken ct = default)
    {
        await SetAsync(Keys.CompanyName, s.CompanyName, ct);
        await SetAsync(Keys.CompanyAddressLine1, s.CompanyAddressLine1, ct);
        await SetAsync(Keys.CompanyAddressLine2, s.CompanyAddressLine2, ct);
        await SetAsync(Keys.CompanyCity, s.CompanyCity, ct);
        await SetAsync(Keys.CompanyState, s.CompanyState, ct);
        await SetAsync(Keys.CompanyPincode, s.CompanyPincode, ct);
        await SetAsync(Keys.CompanyPhone, s.CompanyPhone, ct);
        await SetAsync(Keys.CompanyEmail, s.CompanyEmail, ct);
        await SetAsync(Keys.CompanyGstin, s.CompanyGSTIN, ct);
        await SetAsync(Keys.CompanyPan, s.CompanyPAN, ct);
        await SetAsync(Keys.CompanyLogoPath, s.CompanyLogoPath, ct);
        await SetAsync(Keys.InwardNumberPrefix, s.InwardNumberPrefix, ct);
        await SetAsync(Keys.DcNumberPrefix, s.DcNumberPrefix, ct);
        await SetAsync(Keys.CustomerNumberPrefix, s.CustomerNumberPrefix, ct);
        await SetAsync(Keys.VendorNumberPrefix, s.VendorNumberPrefix, ct);
        await SetAsync(Keys.ItemNumberPrefix, s.ItemNumberPrefix, ct);
        await SetAsync(Keys.CategoryNumberPrefix, s.CategoryNumberPrefix, ct);
        await SetAsync(Keys.FooterNote, s.FooterNote, ct);
        await SetAsync(Keys.RequireSerialForTrackedItems, s.RequireSerialForTrackedItems.ToString(), ct);

        return OperationResult.Ok("Company settings saved.");
    }

    public async Task<IReadOnlyList<SettingDto>> GetAllAsync(CancellationToken ct = default)
    {
        var settings = await _uow.Settings.GetAllAsync(ct);
        return settings.Select(x => new SettingDto
        {
            Key = x.Key,
            Value = x.Value,
            Group = x.Group,
            Description = x.Description,
            DataType = x.DataType,
            IsSystem = x.IsSystem
        }).ToList();
    }

    public async Task<OperationResult> SetAsync(string key, string value, CancellationToken ct = default)
    {
        var existing = await _uow.Settings.GetByKeyAsync(key, ct);
        if (existing is null)
        {
            await _uow.Settings.AddAsync(new Setting
            {
                Key = key,
                Value = value,
                Group = "Company",
                DataType = "string",
                ModifiedBy = _currentUser.UserId
            }, ct);
        }
        else
        {
            existing.Value = value;
            existing.ModifiedBy = _currentUser.UserId;
            existing.ModifiedOn = DateTime.UtcNow;
            _uow.Settings.Update(existing);
        }

        await _uow.SaveChangesAsync(ct);
        return OperationResult.Ok("Setting saved.");
    }

    public async Task<string> GetValueAsync(string key, string defaultValue = "", CancellationToken ct = default)
        => await _uow.Settings.GetValueAsync(key, ct) ?? defaultValue;

    private static string Get(IReadOnlyDictionary<string, string> dict, string key, string fallback = "")
        => dict.TryGetValue(key, out var v) ? v : fallback;

    /// <summary>Central list of setting keys. Keeps the codebase free of hardcoded magic strings.</summary>
    public static class Keys
    {
        public const string CompanyName = "Company.Name";
        public const string CompanyAddressLine1 = "Company.AddressLine1";
        public const string CompanyAddressLine2 = "Company.AddressLine2";
        public const string CompanyCity = "Company.City";
        public const string CompanyState = "Company.State";
        public const string CompanyPincode = "Company.Pincode";
        public const string CompanyPhone = "Company.Phone";
        public const string CompanyEmail = "Company.Email";
        public const string CompanyGstin = "Company.GSTIN";
        public const string CompanyPan = "Company.PAN";
        public const string CompanyLogoPath = "Company.LogoPath";
        public const string InwardNumberPrefix = "Numbering.InwardPrefix";
        public const string DcNumberPrefix = "Numbering.DcPrefix";
        public const string CustomerNumberPrefix = "Numbering.CustomerPrefix";
        public const string VendorNumberPrefix = "Numbering.VendorPrefix";
        public const string ItemNumberPrefix = "Numbering.ItemPrefix";
        public const string CategoryNumberPrefix = "Numbering.CategoryPrefix";
        public const string FooterNote = "Documents.FooterNote";
        public const string RequireSerialForTrackedItems = "Documents.RequireSerialForTrackedItems";
    }
}
