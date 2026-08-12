using InwardDC.App.Services;
using InwardDC.App.Views;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Criteria;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace InwardDC.App.ViewModels;

public partial class PurposesViewModel : MasterListViewModelBase<PurposeDto>
{
    private readonly IPurposeService _purposes;
    private readonly IServiceProvider _provider;

    public PurposesViewModel(ICurrentUserService currentUser, IDialogService dialogs,
        IPurposeService purposes, IServiceProvider provider)
        : base(currentUser, dialogs, p => p.Id, id => purposes.DeleteAsync(id), "purpose")
    {
        _purposes = purposes;
        _provider = provider;
        Title = "Purposes";
    }

    protected override async Task<PagedResponse<PurposeDto>> FetchAsync(string searchText, int page, int pageSize, CancellationToken ct)
    {
        var filter = new PurposeSearchFilter
        {
            Page = page,
            PageSize = pageSize,
            SearchText = searchText,
            SortBy = "name"
        };
        return await _purposes.GetPagedAsync(filter, ct);
    }

    [RelayCommand]
    private Task NewAsync() => OpenEditorAsync(null);

    [RelayCommand]
    private Task OpenAsync(PurposeDto? item)
    {
        if (item is null) return Task.CompletedTask;
        return OpenEditorAsync(item.Id);
    }

    private async Task OpenEditorAsync(Guid? id)
    {
        var vm = _provider.GetRequiredService<PurposeEditorViewModel>();
        await vm.InitializeAsync(id);

        var window = new PurposeEditorWindow { DataContext = vm, Owner = System.Windows.Application.Current.MainWindow };
        var saved = _provider.GetRequiredService<IDialogService>().ShowDialog(window);
        if (saved)
            await RefreshAsync();
    }
}
