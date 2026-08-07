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

public partial class CustomersViewModel : MasterListViewModelBase<CustomerDto>
{
    private readonly ICustomerService _customers;
    private readonly IServiceProvider _provider;

    public CustomersViewModel(ICurrentUserService currentUser, IDialogService dialogs,
        ICustomerService customers, IServiceProvider provider)
        : base(currentUser, dialogs, c => c.Id, id => customers.DeleteAsync(id), "customer")
    {
        _customers = customers;
        _provider = provider;
        Title = "Customers";
    }

    protected override async Task<PagedResponse<CustomerDto>> FetchAsync(string searchText, int page, int pageSize, CancellationToken ct)
    {
        var filter = new CustomerSearchFilter
        {
            Page = page,
            PageSize = pageSize,
            SearchText = searchText,
            SortBy = "name"
        };
        return await _customers.GetPagedAsync(filter, ct);
    }

    [RelayCommand]
    private Task NewAsync() => OpenEditorAsync(null);

    [RelayCommand]
    private Task OpenAsync(CustomerDto? item)
    {
        if (item is null) return Task.CompletedTask;
        return OpenEditorAsync(item.Id);
    }

    private async Task OpenEditorAsync(Guid? id)
    {
        var vm = _provider.GetRequiredService<CustomerEditorViewModel>();
        await vm.InitializeAsync(id);

        var window = new CustomerEditorWindow { DataContext = vm, Owner = System.Windows.Application.Current.MainWindow };
        var saved = _provider.GetRequiredService<IDialogService>().ShowDialog(window);
        if (saved)
            await RefreshAsync();
    }
}
