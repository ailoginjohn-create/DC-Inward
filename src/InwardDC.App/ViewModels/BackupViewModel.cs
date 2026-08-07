using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.App.Services;
using InwardDC.Application.Common;
using InwardDC.Application.Interfaces;

namespace InwardDC.App.ViewModels;

public partial class BackupViewModel : ViewModelBase
{
    private readonly IBackupService _backup;
    private readonly IDialogService _dialogs;

    public BackupViewModel(ICurrentUserService currentUser, IBackupService backup,
        IDialogService dialogs) : base(currentUser)
    {
        _backup = backup;
        _dialogs = dialogs;
        Title = "Backup & Restore";
    }

    public ObservableCollection<string> Backups { get; } = new();

    public override Task OnNavigatedAsync(CancellationToken ct = default)
        => RefreshListAsync(ct);

    private async Task RefreshListAsync(CancellationToken ct = default)
    {
        await RunAsync(async () =>
        {
            Backups.Clear();
            foreach (var file in await _backup.ListBackupsAsync(ct))
                Backups.Add(file);
        }, "Listing backups...", ct);
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        await RunAsync(async () =>
        {
            var file = await _backup.CreateBackupAsync();
            _dialogs.ShowInfo($"Backup created:\n{file}");
            await RefreshListAsync();
        }, "Creating backup...");
    }

    [RelayCommand]
    private async Task RestoreAsync(string? file)
    {
        var path = file;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = _dialogs.PickOpenFile("Backup Archives|*.zip");
            if (path is null) return;
        }

        if (!_dialogs.Confirm(
                $"Restore from:\n{path}\n\nAll current data will be replaced. Continue?",
                "Restore Backup"))
            return;

        await RunAsync(async () =>
        {
            var result = await _backup.RestoreAsync(path);
            if (result.Success)
                _dialogs.ShowInfo(result.Message, "Restore Complete");
            else
                _dialogs.ShowError(result.Message, "Restore Failed");
            await RefreshListAsync();
        }, "Restoring...");
    }

    [RelayCommand]
    private async Task FactoryResetAsync()
    {
        if (!_dialogs.Confirm(
                "Factory reset will DELETE all business data and restore default settings.\n\n" +
                "A safety backup will be attempted first. Continue?",
                "Factory Reset"))
            return;

        await RunAsync(async () =>
        {
            var result = await _backup.FactoryResetAsync();
            if (result.Success)
                _dialogs.ShowInfo(result.Message, "Factory Reset");
            else
                _dialogs.ShowError(result.Message, "Factory Reset Failed");
            await RefreshListAsync();
        }, "Resetting...");
    }
}
