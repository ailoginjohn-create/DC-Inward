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

public partial class ItemCategoriesViewModel : MasterListViewModelBase<ItemCategoryDto>
{
    private readonly IItemCategoryService _categories;
    private readonly IServiceProvider _provider;

    public ItemCategoriesViewModel(ICurrentUserService currentUser, IDialogService dialogs,
        IItemCategoryService categories, IServiceProvider provider)
        : base(currentUser, dialogs, c => c.Id, id => categories.DeleteAsync(id), "category")
    {
        _categories = categories;
        _provider = provider;
        Title = "Item Categories";
    }

    protected override async Task<PagedResponse<ItemCategoryDto>> FetchAsync(string searchText, int page, int pageSize, CancellationToken ct)
    {
        var filter = new ItemCategorySearchFilter
        {
            Page = page,
            PageSize = pageSize,
            SearchText = searchText,
            SortBy = "name"
        };
        return await _categories.GetPagedAsync(filter, ct);
    }

    [RelayCommand]
    private Task NewAsync() => OpenEditorAsync(null);

    [RelayCommand]
    private Task OpenAsync(ItemCategoryDto? item)
    {
        if (item is null) return Task.CompletedTask;
        return OpenEditorAsync(item.Id);
    }

    private async Task OpenEditorAsync(Guid? id)
    {
        var vm = _provider.GetRequiredService<ItemCategoryEditorViewModel>();
        await vm.InitializeAsync(id);

        var window = new ItemCategoryEditorWindow { DataContext = vm, Owner = System.Windows.Application.Current.MainWindow };
        var saved = _provider.GetRequiredService<IDialogService>().ShowDialog(window);
        if (saved)
            await RefreshAsync();
    }
}
