using System.Windows;
using CommunityToolkit.Mvvm.Input;
using InwardDC.App.Services;
using InwardDC.App.Views;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Criteria;
using Microsoft.Extensions.DependencyInjection;

namespace InwardDC.App.ViewModels;

public partial class ItemsViewModel : MasterListViewModelBase<ItemDto>
{
    private readonly IItemService _items;
    private readonly IServiceProvider _provider;

    public ItemsViewModel(ICurrentUserService currentUser, IDialogService dialogs,
        IItemService items, IServiceProvider provider)
        : base(currentUser, dialogs, i => i.Id, id => items.DeleteAsync(id), "item")
    {
        _items = items;
        _provider = provider;
        Title = "Items";
    }

    protected override async Task<PagedResponse<ItemDto>> FetchAsync(string searchText, int page, int pageSize, CancellationToken ct)
    {
        var filter = new ItemSearchFilter
        {
            Page = page,
            PageSize = pageSize,
            SearchText = searchText,
            SortBy = "name"
        };
        return await _items.GetPagedAsync(filter, ct);
    }

    [RelayCommand]
    private Task NewAsync() => OpenEditorAsync(null);

    [RelayCommand]
    private Task OpenAsync(ItemDto? item)
    {
        if (item is null) return Task.CompletedTask;
        return OpenEditorAsync(item.Id);
    }

    private async Task OpenEditorAsync(Guid? id)
    {
        var vm = _provider.GetRequiredService<ItemEditorViewModel>();
        await vm.InitializeAsync(id);

        var window = new ItemEditorWindow { DataContext = vm, Owner = System.Windows.Application.Current.MainWindow };
        var saved = _provider.GetRequiredService<IDialogService>().ShowDialog(window);
        if (saved)
            await RefreshAsync();
    }
}
