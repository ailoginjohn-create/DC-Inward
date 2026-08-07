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

public partial class VendorsViewModel : MasterListViewModelBase<VendorDto>
{
    private readonly IVendorService _vendors;
    private readonly IServiceProvider _provider;

    public VendorsViewModel(ICurrentUserService currentUser, IDialogService dialogs,
        IVendorService vendors, IServiceProvider provider)
        : base(currentUser, dialogs, v => v.Id, id => vendors.DeleteAsync(id), "vendor")
    {
        _vendors = vendors;
        _provider = provider;
        Title = "Vendors";
    }

    protected override async Task<PagedResponse<VendorDto>> FetchAsync(string searchText, int page, int pageSize, CancellationToken ct)
    {
        var filter = new VendorSearchFilter
        {
            Page = page,
            PageSize = pageSize,
            SearchText = searchText,
            SortBy = "name"
        };
        return await _vendors.GetPagedAsync(filter, ct);
    }

    [RelayCommand]
    private Task NewAsync() => OpenEditorAsync(null);

    [RelayCommand]
    private Task OpenAsync(VendorDto? item)
    {
        if (item is null) return Task.CompletedTask;
        return OpenEditorAsync(item.Id);
    }

    private async Task OpenEditorAsync(Guid? id)
    {
        var vm = _provider.GetRequiredService<VendorEditorViewModel>();
        await vm.InitializeAsync(id);

        var window = new VendorEditorWindow { DataContext = vm, Owner = System.Windows.Application.Current.MainWindow };
        var saved = _provider.GetRequiredService<IDialogService>().ShowDialog(window);
        if (saved)
            await RefreshAsync();
    }
}
