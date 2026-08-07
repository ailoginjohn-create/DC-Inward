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

public partial class UsersViewModel : MasterListViewModelBase<UserDto>
{
    private readonly IUserService _users;
    private readonly IServiceProvider _provider;

    public UsersViewModel(ICurrentUserService currentUser, IDialogService dialogs,
        IUserService users, IServiceProvider provider)
        : base(currentUser, dialogs, u => u.Id, id => users.DeleteAsync(id), "user")
    {
        _users = users;
        _provider = provider;
        Title = "Users";
    }

    protected override async Task<PagedResponse<UserDto>> FetchAsync(string searchText, int page, int pageSize, CancellationToken ct)
    {
        var filter = new UserSearchFilter
        {
            Page = page,
            PageSize = pageSize,
            SearchText = searchText,
            SortBy = "name"
        };
        return await _users.GetPagedAsync(filter, ct);
    }

    [RelayCommand]
    private Task NewAsync() => OpenEditorAsync(null);

    [RelayCommand]
    private Task OpenAsync(UserDto? item)
    {
        if (item is null) return Task.CompletedTask;
        return OpenEditorAsync(item.Id);
    }

    private async Task OpenEditorAsync(Guid? id)
    {
        var vm = _provider.GetRequiredService<UserEditorViewModel>();
        await vm.InitializeAsync(id);

        var window = new UserEditorWindow { DataContext = vm, Owner = System.Windows.Application.Current.MainWindow };
        var saved = _provider.GetRequiredService<IDialogService>().ShowDialog(window);
        if (saved)
            await RefreshAsync();
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(UserDto? item)
    {
        if (item is null) return;

        var result = item.IsActive
            ? await _users.DisableAsync(item.Id)
            : await _users.EnableAsync(item.Id);

        if (result.Success)
            SetSuccess(result.Message);
        else
            _provider.GetRequiredService<IDialogService>().ShowError(result.Message);

        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ResetPasswordAsync(UserDto? item)
    {
        if (item is null) return;

        var dialogs = _provider.GetRequiredService<IDialogService>();
        var newPassword = "ChangeMe@123";
        if (!dialogs.Confirm(
                $"Reset password for {item.FullName} to the temporary password '{newPassword}'?",
                "Reset Password"))
            return;

        var result = await _users.ResetPasswordAsync(new ResetPasswordRequest
        {
            UserId = item.Id,
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        });

        if (result.Success)
            dialogs.ShowInfo(result.Message, "Reset Password");
        else
            dialogs.ShowError(result.Message);
    }
}
