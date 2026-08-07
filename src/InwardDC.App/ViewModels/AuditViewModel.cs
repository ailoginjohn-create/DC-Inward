using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.App.Services;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Criteria;
using InwardDC.Domain.Enums;

namespace InwardDC.App.ViewModels;

public partial class AuditViewModel : ViewModelBase
{
    private readonly IAuditService _audit;
    private readonly IExcelService _excel;
    private readonly IDialogService _dialogs;

    public AuditViewModel(ICurrentUserService currentUser, IAuditService audit,
        IExcelService excel, IDialogService dialogs) : base(currentUser)
    {
        _audit = audit;
        _excel = excel;
        _dialogs = dialogs;
        Title = "Audit Log";
    }

    public ObservableCollection<AuditLogDto> Items { get; } = new();

    public IReadOnlyList<AuditAction> Actions => Enum.GetValues<AuditAction>();

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private AuditAction? _action;
    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _pageSize = 50;

    public bool CanGoBack => Page > 1;
    public bool CanGoNext => Page * PageSize < TotalCount;

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
            var filter = new AuditLogFilter
            {
                Page = Page,
                PageSize = PageSize,
                SearchText = SearchText,
                Action = Action,
                FromDate = FromDate,
                ToDate = ToDate,
                SortBy = "date",
                SortDescending = true
            };

            var result = await _audit.GetPagedAsync(filter, ct);
            Items.Clear();
            foreach (var item in result.Items)
                Items.Add(item);

            TotalCount = result.TotalCount;
            Page = result.Page;
            PageSize = result.PageSize;

            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoNext));
            SetSuccess($"{TotalCount} audit record(s).");
        }, "Loading audit log...", ct);
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var path = _dialogs.PickSaveFile("Excel Workbook|*.xlsx", $"AuditLog_{DateTime.Today:yyyyMMdd}.xlsx");
        if (path is null) return;

        await RunAsync(async () =>
        {
            var file = await _excel.ExportAuditLogsAsync(Items.ToList(), path);
            _dialogs.ShowInfo($"Exported to:\n{file}");
        }, "Exporting...");
    }
}
