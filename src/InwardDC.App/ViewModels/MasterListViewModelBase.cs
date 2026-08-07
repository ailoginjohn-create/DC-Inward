using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.App.Services;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;

namespace InwardDC.App.ViewModels;

/// <summary>
/// Shared paged-list behaviour for the master screens (customers, vendors, items,
/// categories, users). Derived types supply the query and the delete command.
/// </summary>
public abstract partial class MasterListViewModelBase<TDto> : ViewModelBase
    where TDto : class
{
    private readonly IDialogService _dialogs;
    private readonly Func<TDto, Guid> _idOf;
    private readonly Func<Guid, Task<OperationResult>> _delete;
    private readonly string _entityName;

    protected MasterListViewModelBase(
        ICurrentUserService currentUser,
        IDialogService dialogs,
        Func<TDto, Guid> idOf,
        Func<Guid, Task<OperationResult>> delete,
        string entityName)
        : base(currentUser)
    {
        _dialogs = dialogs;
        _idOf = idOf;
        _delete = delete;
        _entityName = entityName;
        Items = new ObservableCollection<TDto>();
    }

    public ObservableCollection<TDto> Items { get; }

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _pageSize = 50;
    [ObservableProperty] private TDto? _selectedItem;

    public bool CanGoBack => Page > 1;
    public bool CanGoNext => Page * PageSize < TotalCount;

    protected abstract Task<PagedResponse<TDto>> FetchAsync(string searchText, int page, int pageSize, CancellationToken ct);

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

    protected async Task RefreshAsync(CancellationToken ct = default)
    {
        await RunAsync(async () =>
        {
            var result = await FetchAsync(SearchText, Page, PageSize, ct);
            Items.Clear();
            foreach (var item in result.Items)
                Items.Add(item);

            TotalCount = result.TotalCount;
            Page = result.Page;
            PageSize = result.PageSize;

            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoNext));
            SetSuccess($"{TotalCount} {_entityName} record(s).");
        }, $"Loading {_entityName}...", ct);
    }

    [RelayCommand]
    protected async Task DeleteAsync(TDto? item)
    {
        if (item is null) return;

        if (!_dialogs.Confirm($"Delete {_entityName}? This cannot be undone.", $"Delete {_entityName}"))
            return;

        var result = await _delete(_idOf(item));
        if (result.Success)
            SetSuccess(result.Message);
        else
            _dialogs.ShowError(result.Message);

        await RefreshAsync();
    }
}
