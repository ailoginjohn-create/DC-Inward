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

public partial class DispatchListViewModel : ViewModelBase
{
    private readonly IDispatchService _dispatch;
    private readonly IExcelService _excel;
    private readonly IPdfService _pdf;
    private readonly IDialogService _dialogs;
    private readonly IServiceProvider _provider;

    public DispatchListViewModel(ICurrentUserService currentUser, IDispatchService dispatch,
        IExcelService excel, IPdfService pdf, IDialogService dialogs, IServiceProvider provider)
        : base(currentUser)
    {
        _dispatch = dispatch;
        _excel = excel;
        _pdf = pdf;
        _dialogs = dialogs;
        _provider = provider;
        Title = "Dispatch Challans";
        Items = new ObservableCollection<DispatchDto>();
    }

    public ObservableCollection<DispatchDto> Items { get; }

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private DispatchStatus? _status;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _pageSize = 50;
    [ObservableProperty] private DispatchDto? _selectedItem;

    public bool CanGoBack => Page > 1;
    public bool CanGoNext => Page * PageSize < TotalCount;

    public IReadOnlyList<DispatchStatus> Statuses => Enum.GetValues<DispatchStatus>();

    public override Task OnNavigatedAsync(CancellationToken ct = default) => RefreshAsync(ct);

    [RelayCommand] private Task SearchAsync() => RefreshAsync();
    [RelayCommand] private Task FirstPageAsync() => GoToPageAsync(1);
    [RelayCommand] private Task PrevPageAsync() => GoToPageAsync(Page - 1);
    [RelayCommand] private Task NextPageAsync() => GoToPageAsync(Page + 1);

    private Task GoToPageAsync(int page)
    {
        Page = Math.Max(1, page);
        return RefreshAsync();
    }

    private async Task RefreshAsync(CancellationToken ct = default)
    {
        await RunAsync(async () =>
        {
            var filter = new DispatchSearchFilter
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

            var result = await _dispatch.GetPagedAsync(filter, ct);
            Items.Clear();
            foreach (var item in result.Items)
                Items.Add(item);

            TotalCount = result.TotalCount;
            Page = result.Page;
            PageSize = result.PageSize;

            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoNext));
            SetSuccess($"{TotalCount} dispatch challan(s).");
        }, "Loading dispatch challans...", ct);
    }

    [RelayCommand]
    private Task NewAsync() => OpenEditorAsync();

    private async Task OpenEditorAsync()
    {
        var vm = _provider.GetRequiredService<DispatchEditorViewModel>();
        await vm.InitializeAsync();

        var window = new DispatchEditorWindow { DataContext = vm, Owner = System.Windows.Application.Current.MainWindow };
        var saved = _dialogs.ShowDialog(window);
        if (saved)
            await RefreshAsync();
    }

    [RelayCommand]
    private async Task PdfAsync(DispatchDto? item)
    {
        if (item is null) return;

        var path = _dialogs.PickSaveFile("PDF Document|*.pdf", $"{item.DcNo}.pdf");
        if (path is null) return;

        await RunAsync(async () =>
        {
            var file = await _pdf.GenerateDcPdfAsync(item.Id, path);
            _dialogs.ShowInfo($"PDF saved to:\n{file}");
        }, "Generating PDF...");
    }

    [RelayCommand]
    private async Task CancelAsync(DispatchDto? item)
    {
        if (item is null) return;

        if (!_dialogs.Confirm($"Cancel dispatch {item.DcNo}? Stock will be returned to available.", "Cancel Dispatch"))
            return;

        var result = await _dispatch.CancelAsync(item.Id);
        if (result.Success)
            SetSuccess(result.Message);
        else
            _dialogs.ShowError(result.Message);

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(DispatchDto? item)
    {
        if (item is null) return;

        if (!_dialogs.Confirm($"Delete dispatch {item.DcNo}? This cannot be undone.", "Delete Dispatch"))
            return;

        var result = await _dispatch.DeleteAsync(item.Id);
        if (result.Success)
            SetSuccess(result.Message);
        else
            _dialogs.ShowError(result.Message);

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task TemplateAsync()
    {
        var path = _dialogs.PickSaveFile("Excel Workbook|*.xlsx", "DispatchImportTemplate.xlsx");
        if (path is null) return;

        await using var stream = await _excel.CreateDispatchImportTemplateAsync();
        await using var fs = File.Create(path);
        await stream.CopyToAsync(fs);

        _dialogs.ShowInfo($"Dispatch import template saved to:\n{path}");
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var path = _dialogs.PickOpenFile("Excel Workbook|*.xlsx");
        if (path is null) return;

        await RunAsync(async () =>
        {
            await using var stream = File.OpenRead(path);
            var result = await _excel.ImportDispatchesAsync(stream, Path.GetFileName(path));
            _dialogs.ShowInfo(result.Summary, "Import Result");
            await RefreshAsync();
        }, "Importing...");
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var path = _dialogs.PickSaveFile("Excel Workbook|*.xlsx", $"Dispatches_{DateTime.Today:yyyyMMdd}.xlsx");
        if (path is null) return;

        var filter = new DispatchSearchFilter
        {
            SearchText = SearchText,
            FromDate = FromDate,
            ToDate = ToDate,
            Status = Status
        };

        await RunAsync(async () =>
        {
            var file = await _excel.ExportDispatchesAsync(filter, path);
            _dialogs.ShowInfo($"Exported to:\n{file}");
        }, "Exporting...");
    }
}
