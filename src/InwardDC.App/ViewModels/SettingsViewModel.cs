using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.App.Services;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;

namespace InwardDC.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly IDialogService _dialogs;
    private readonly IUpdateService _updates;
    private UpdateInfo? _pendingUpdate;

    public SettingsViewModel(ICurrentUserService currentUser, ISettingsService settings,
        IDialogService dialogs, IUpdateService updates) : base(currentUser)
    {
        _settings = settings;
        _dialogs = dialogs;
        _updates = updates;
        Title = "Company Settings";
    }

    [ObservableProperty] private string _companyName = string.Empty;
    [ObservableProperty] private string _companyAddressLine1 = string.Empty;
    [ObservableProperty] private string _companyAddressLine2 = string.Empty;
    [ObservableProperty] private string _companyCity = string.Empty;
    [ObservableProperty] private string _companyState = string.Empty;
    [ObservableProperty] private string _companyPincode = string.Empty;
    [ObservableProperty] private string _companyPhone = string.Empty;
    [ObservableProperty] private string _companyEmail = string.Empty;
    [ObservableProperty] private string _companyGstin = string.Empty;
    [ObservableProperty] private string _companyPan = string.Empty;
    [ObservableProperty] private string _companyLogoPath = string.Empty;
    [ObservableProperty] private string _inwardNumberPrefix = "INW";
    [ObservableProperty] private string _dcNumberPrefix = "DC";
    [ObservableProperty] private string _customerNumberPrefix = "CUS";
    [ObservableProperty] private string _vendorNumberPrefix = "VEN";
    [ObservableProperty] private string _itemNumberPrefix = "ITM";
    [ObservableProperty] private string _categoryNumberPrefix = "CAT";
    [ObservableProperty] private string _footerNote = string.Empty;
    [ObservableProperty] private bool _requireSerialForTrackedItems = true;
    [ObservableProperty] private string _currentAppVersion = string.Empty;
    [ObservableProperty] private string _updateStatus = string.Empty;
    [ObservableProperty] private string _updateNotes = string.Empty;
    [ObservableProperty] private bool _updateAvailable;

    public override Task OnNavigatedAsync(CancellationToken ct = default)
    {
        CurrentAppVersion = _updates.CurrentVersion;
        return RunAsync(LoadAsync, "Loading settings...", ct);
    }

    private async Task LoadAsync()
    {
        var s = await _settings.GetCompanySettingsAsync();
        CompanyName = s.CompanyName;
        CompanyAddressLine1 = s.CompanyAddressLine1;
        CompanyAddressLine2 = s.CompanyAddressLine2;
        CompanyCity = s.CompanyCity;
        CompanyState = s.CompanyState;
        CompanyPincode = s.CompanyPincode;
        CompanyPhone = s.CompanyPhone;
        CompanyEmail = s.CompanyEmail;
        CompanyGstin = s.CompanyGSTIN;
        CompanyPan = s.CompanyPAN;
        CompanyLogoPath = s.CompanyLogoPath;
        InwardNumberPrefix = s.InwardNumberPrefix;
        DcNumberPrefix = s.DcNumberPrefix;
        CustomerNumberPrefix = s.CustomerNumberPrefix;
        VendorNumberPrefix = s.VendorNumberPrefix;
        ItemNumberPrefix = s.ItemNumberPrefix;
        CategoryNumberPrefix = s.CategoryNumberPrefix;
        FooterNote = s.FooterNote;
        RequireSerialForTrackedItems = s.RequireSerialForTrackedItems;
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        UpdateStatus = "Checking for updates...";
        UpdateNotes = string.Empty;
        UpdateAvailable = false;
        _pendingUpdate = null;

        await RunAsync(async () =>
        {
            var info = await _updates.CheckForUpdateAsync();
            if (info is null)
            {
                UpdateStatus = $"You are up to date (version {_updates.CurrentVersion}).";
                return;
            }

            _pendingUpdate = info;
            UpdateAvailable = true;
            UpdateStatus = $"Version {info.Version} is available. Current: {_updates.CurrentVersion}.";
            UpdateNotes = info.Notes ?? string.Empty;
        }, "Checking for updates...");
    }

    [RelayCommand]
    private async Task DownloadAndInstallAsync()
    {
        if (_pendingUpdate is null)
        {
            _dialogs.ShowInfo("No update is pending. Click 'Check for Updates' first.");
            return;
        }

        if (!_dialogs.Confirm(
                $"Download version {_pendingUpdate.Version} now?\n\nThe app will close and reopen automatically after downloading.",
                "Install Update"))
            return;

        await RunAsync(async () =>
        {
            UpdateStatus = "Downloading update...";
            var path = await _updates.DownloadUpdateAsync(_pendingUpdate);
            if (_updates.ApplyUpdate(path))
                System.Windows.Application.Current.Shutdown();
            else
                _dialogs.ShowError("The update was downloaded but could not be applied automatically. Please download it again manually.");
        }, "Downloading update...");
    }

    [RelayCommand]
    private void PickLogo()
    {
        var path = _dialogs.PickOpenFile("Images|*.png;*.jpg;*.jpeg;*.bmp");
        if (path is not null)
            CompanyLogoPath = path;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await RunAsync(async () =>
        {
            var settings = new CompanySettingsDto
            {
                CompanyName = CompanyName,
                CompanyAddressLine1 = CompanyAddressLine1,
                CompanyAddressLine2 = CompanyAddressLine2,
                CompanyCity = CompanyCity,
                CompanyState = CompanyState,
                CompanyPincode = CompanyPincode,
                CompanyPhone = CompanyPhone,
                CompanyEmail = CompanyEmail,
                CompanyGSTIN = CompanyGstin,
                CompanyPAN = CompanyPan,
                CompanyLogoPath = CompanyLogoPath,
                InwardNumberPrefix = InwardNumberPrefix,
                DcNumberPrefix = DcNumberPrefix,
                CustomerNumberPrefix = CustomerNumberPrefix,
                VendorNumberPrefix = VendorNumberPrefix,
                ItemNumberPrefix = ItemNumberPrefix,
                CategoryNumberPrefix = CategoryNumberPrefix,
                FooterNote = FooterNote,
                RequireSerialForTrackedItems = RequireSerialForTrackedItems
            };

            var result = await _settings.SaveCompanySettingsAsync(settings);
            if (result.Success)
                SetSuccess(result.Message);
            else
                ShowError(new Domain.Exceptions.DomainException(result.Message));
        }, "Saving settings...");
    }
}
