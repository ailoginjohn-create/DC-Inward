using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.App.Services;
using InwardDC.Application.Common;
using InwardDC.Domain.Catalog;

namespace InwardDC.App.ViewModels;

/// <summary>A single entry in the left navigation rail.</summary>
public sealed record NavItem(string Label, Type ViewModelType, string ModuleKey, bool RequiresAdmin = false);

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

        RefreshNavItems();
    }

    public event Action? SignOutRequested;

    /// <summary>Every module regardless of the signed-in user's permissions.</summary>
    public IReadOnlyList<NavItem> AllNavItems { get; } = BuildAllNavItems();

    /// <summary>The modules the signed-in user may open, in navigation order.</summary>
    public ObservableCollection<NavItem> NavItems { get; } = new();

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

    /// <summary>
    /// Rebuilds <see cref="NavItems"/> from the signed-in user's module access and
    /// returns the shell to the Dashboard. Called when the shell opens so sign-outs
    /// followed by a different login show the correct set of modules.
    /// </summary>
    public void RefreshNavItems()
    {
        NavItems.Clear();
        foreach (var item in AllNavItems.Where(i => CurrentUser.CanAccessModule(i.ModuleKey)))
            NavItems.Add(item);

        SelectedNavItem = NavItems.Count > 0 ? NavItems[0] : null;
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
        if (!CurrentUser.CanAccessModule(item.ModuleKey))
        {
            _dialogs.ShowWarning("You do not have access to this module.");
            return;
        }

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

    private static readonly Dictionary<string, Type> ModuleViewModels = new()
    {
        [AppModule.Dashboard.Key] = typeof(DashboardViewModel),
        [AppModule.Inward.Key] = typeof(InwardListViewModel),
        [AppModule.Dispatch.Key] = typeof(DispatchListViewModel),
        [AppModule.Customers.Key] = typeof(CustomersViewModel),
        [AppModule.Vendors.Key] = typeof(VendorsViewModel),
        [AppModule.Items.Key] = typeof(ItemsViewModel),
        [AppModule.ItemCategories.Key] = typeof(ItemCategoriesViewModel),
        [AppModule.Purposes.Key] = typeof(PurposesViewModel),
        [AppModule.Search.Key] = typeof(SearchViewModel),
        [AppModule.Reports.Key] = typeof(ReportsViewModel),
        [AppModule.Users.Key] = typeof(UsersViewModel),
        [AppModule.Settings.Key] = typeof(SettingsViewModel),
        [AppModule.Backup.Key] = typeof(BackupViewModel),
        [AppModule.Audit.Key] = typeof(AuditViewModel)
    };

    private static List<NavItem> BuildAllNavItems() =>
        AppModule.All
            .Select(m => new NavItem(m.Label, ModuleViewModels[m.Key], m.Key, RequiresAdmin: m.AdminOnly))
            .ToList();
}
