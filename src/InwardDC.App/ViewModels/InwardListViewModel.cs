using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.App.Services;
using InwardDC.App.Views;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Criteria;
using InwardDC.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace InwardDC.App.ViewModels;

public partial class InwardListViewModel : ViewModelBase
{
    private readonly IInwardService _inward;
    private readonly IExcelService _excel;
    private readonly IPdfService _pdf;
    private readonly IDialogService _dialogs;
    private readonly IServiceProvider _provider;

    public InwardListViewModel(ICurrentUserService currentUser, IInwardService inward,
        IExcelService excel, IPdfService pdf, IDialogService dialogs, IServiceProvider provider)
        : base(currentUser)
    {
        _inward = inward;
        _excel = excel;
        _pdf = pdf;
        _dialogs = dialogs;
        _provider = provider;
        Title = "Inward";
        Items = new ObservableCollection<InwardDto>();
    }

    public ObservableCollection<InwardDto> Items { get; }

    public IReadOnlyList<InwardStatus> Statuses => Enum.GetValues<InwardStatus>();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private DateTime? _fromDate;

    [ObservableProperty]
    private DateTime? _toDate;

    [ObservableProperty]
    private InwardStatus? _status;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _page = 1;

    [ObservableProperty]
    private int _pageSize = 50;

    [ObservableProperty]
    private InwardDto? _selectedItem;

    public bool CanGoBack => Page > 1;
    public bool CanGoNext => Page * PageSize < TotalCount;

    public override Task OnNavigatedAsync(CancellationToken ct = default)
        => RefreshAsync(ct);

    [RelayCommand]
    private Task SearchAsync() => RefreshAsync();

    [RelayCommand]
    private Task FirstPageAsync() => GoToPageAsync(1);

    [RelayCommand]
    private Task PrevPageAsync() => GoToPageAsync(Page - 1);

    [RelayCommand]
    private Task NextPageAsync() => GoToPageAsync(Page + 1);

    private Task GoToPageAsync(int page)
    {
        Page = Math.Max(1, page);
        return RefreshAsync();
    }

    private async Task RefreshAsync(CancellationToken ct = default)
    {
        await RunAsync(async () =>
        {
            var filter = new InwardSearchFilter
            {
                Page = Page,
                PageSize = PageSize,
                SearchText = SearchText,
                FromDate = FromDate,
                ToDate = ToDate,
                Status = Status,
                SortBy = "date",
                SortDescending = true
            };

            var result = await _inward.GetPagedAsync(filter, ct);
            Items.Clear();
            foreach (var item in result.Items)
                Items.Add(item);

            TotalCount = result.TotalCount;
            Page = result.Page;
            PageSize = result.PageSize;

            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoNext));
            SetSuccess($"{TotalCount} inward record(s).");
        }, "Loading inwards...", ct);
    }

    [RelayCommand]
    private Task NewAsync() => OpenEditorAsync(null);

    [RelayCommand]
    private Task OpenAsync(InwardDto? item)
    {
        if (item is null) return Task.CompletedTask;
        return OpenEditorAsync(item.Id);
    }

    private async Task OpenEditorAsync(Guid? id)
    {
        var vm = _provider.GetRequiredService<InwardEditorViewModel>();
        await vm.InitializeAsync(id);

        var window = new InwardEditorWindow { DataContext = vm, Owner = System.Windows.Application.Current.MainWindow };
        var saved = _dialogs.ShowDialog(window);
        if (saved)
            await RefreshAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(InwardDto? item)
    {
        if (item is null) return;

        if (!_dialogs.Confirm($"Delete inward {item.InwardNo}? This cannot be undone.", "Delete Inward"))
            return;

        var result = await _inward.DeleteAsync(item.Id);
        if (result.Success)
            SetSuccess(result.Message);
        else
            _dialogs.ShowError(result.Message);

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task CancelAsync(InwardDto? item)
    {
        if (item is null) return;

        if (!_dialogs.Confirm($"Cancel inward {item.InwardNo}?", "Cancel Inward"))
            return;

        var result = await _inward.UpdateStatusAsync(new InwardStatusRequest
        {
            InwardId = item.Id,
            Status = InwardStatus.Cancelled
        });

        if (result.Success)
            SetSuccess(result.Message);
        else
            _dialogs.ShowError(result.Message);

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task TemplateAsync()
    {
        var path = _dialogs.PickSaveFile("Excel Workbook|*.xlsx", "InwardImportTemplate.xlsx");
        if (path is null) return;

        await using var stream = await _excel.CreateImportTemplateAsync();
        await using var fs = File.Create(path);
        await stream.CopyToAsync(fs);

        _dialogs.ShowInfo($"Import template saved to:\n{path}");
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var path = _dialogs.PickOpenFile("Excel Workbook|*.xlsx");
        if (path is null) return;

        var progress = new Progress<string>(m => StatusMessage = m);
        IsBusy = true;
        StatusMessage = "Importing...";
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            await using var stream = File.OpenRead(path);
            var result = await _excel.ImportInwardAsync(stream, Path.GetFileName(path), progress: progress);
            _dialogs.ShowInfo(result.Summary, "Import Result");
            SearchText = string.Empty;
            FromDate = null;
            ToDate = null;
            Status = null;
            Page = 1;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            var message = FriendlyMessage(ex);
            _dialogs.ShowError($"Import failed: {message}", "Import Error");
            HasError = true;
            ErrorMessage = message;
        }
        finally
        {
            IsBusy = false;
            StatusMessage = HasError ? ErrorMessage : "Ready";
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var path = _dialogs.PickSaveFile("Excel Workbook|*.xlsx", $"Inwards_{DateTime.Today:yyyyMMdd}.xlsx");
        if (path is null) return;

        var filter = new InwardSearchFilter
        {
            SearchText = SearchText,
            FromDate = FromDate,
            ToDate = ToDate,
            Status = Status
        };

        await RunAsync(async () =>
        {
            var file = await _excel.ExportInwardsAsync(filter, path);
            _dialogs.ShowInfo($"Exported to:\n{file}");
        }, "Exporting...");
    }

    [RelayCommand]
    private async Task PdfAsync(InwardDto? item)
    {
        if (item is null) return;

        var path = _dialogs.PickSaveFile("PDF Document|*.pdf", $"{item.InwardNo}.pdf");
        if (path is null) return;

        await RunAsync(async () =>
        {
            var file = await _pdf.GenerateInwardPdfAsync(item.Id, path);
            _dialogs.ShowInfo($"PDF saved to:\n{file}");
        }, "Generating PDF...");
    }
}
