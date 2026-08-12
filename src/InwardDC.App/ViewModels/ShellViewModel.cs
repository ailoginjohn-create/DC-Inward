using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.App.Services;
using InwardDC.Application.Common;

namespace InwardDC.App.ViewModels;

/// <summary>A single entry in the left navigation rail.</summary>
public sealed record NavItem(string Label, Type ViewModelType, bool RequiresAdmin = false);

public partial class ShellViewModel : ViewModelBase
{
    private readonly IServiceProvider _provider;
    private readonly IDialogService _dialogs;

    public ShellViewModel(ICurrentUserService currentUser, IServiceProvider provider, IDialogService dialogs)
        : base(currentUser)
    {
        _provider = provider;
        _dialogs = dialogs;
        Title = "Inward & DC";
        UserDisplay = $"{currentUser.FullName} ({currentUser.UserName})";
        NavItems = BuildNavItems();

        // Land on the dashboard.
        NavigateTo(NavItems[0]);
    }

    public event Action? SignOutRequested;

    public IReadOnlyList<NavItem> NavItems { get; }

    [ObservableProperty]
    private string _userDisplay = string.Empty;

    [ObservableProperty]
    private object? _currentContent;

    [ObservableProperty]
    private string _currentTitle = "Dashboard";

    [ObservableProperty]
    private NavItem? _selectedNavItem;

    partial void OnSelectedNavItemChanged(NavItem? value)
    {
        if (value is not null)
            NavigateTo(value);
    }

    [RelayCommand]
    private void Navigate(NavItem? item)
    {
        if (item is not null)
            NavigateTo(item);
    }

    [RelayCommand]
    private void SignOut()
    {
        CurrentUser.SignOut();
        SignOutRequested?.Invoke();
    }

    private void NavigateTo(NavItem item)
    {
        if (item.RequiresAdmin && !IsAdmin)
        {
            _dialogs.ShowWarning("This module is restricted to administrators.");
            return;
        }

        var vm = _provider.GetService(item.ViewModelType);
        if (vm is ViewModelBase viewModel)
        {
            CurrentContent = viewModel;
            CurrentTitle = viewModel.Title;
            _ = viewModel.OnNavigatedAsync();
        }
    }

    private List<NavItem> BuildNavItems() => new()
    {
        new("Dashboard", typeof(DashboardViewModel)),
        new("Inward", typeof(InwardListViewModel)),
        new("Dispatch Challans", typeof(DispatchListViewModel)),
        new("Customers", typeof(CustomersViewModel)),
        new("Vendors", typeof(VendorsViewModel)),
        new("Items", typeof(ItemsViewModel)),
        new("Item Categories", typeof(ItemCategoriesViewModel)),
        new("Purposes", typeof(PurposesViewModel)),
        new("Search", typeof(SearchViewModel)),
        new("Reports", typeof(ReportsViewModel)),
        new("Users", typeof(UsersViewModel), RequiresAdmin: true),
        new("Company Settings", typeof(SettingsViewModel)),
        new("Backup & Restore", typeof(BackupViewModel)),
        new("Audit Log", typeof(AuditViewModel))
    };
}
