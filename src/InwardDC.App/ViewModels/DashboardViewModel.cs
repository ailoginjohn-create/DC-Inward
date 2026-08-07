using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;

namespace InwardDC.App.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IDashboardService _dashboard;

    public DashboardViewModel(ICurrentUserService currentUser, IDashboardService dashboard) : base(currentUser)
    {
        _dashboard = dashboard;
        Title = "Dashboard";
    }

    [ObservableProperty]
    private DashboardStatsDto _stats = new();

    [ObservableProperty]
    private string _companyName = string.Empty;

    [ObservableProperty]
    private string _greeting = string.Empty;

    public override Task OnNavigatedAsync(CancellationToken ct = default)
        => RunAsync(LoadAsync, "Loading dashboard...", ct);

    private async Task LoadAsync()
    {
        var stats = await _dashboard.GetStatsAsync();
        Stats = stats;
        Greeting = $"Welcome back, {CurrentUser.FullName}.";
    }
}
