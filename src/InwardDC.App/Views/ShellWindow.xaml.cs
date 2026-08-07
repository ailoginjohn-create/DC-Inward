using System.Windows;
using InwardDC.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace InwardDC.App.Views;

public partial class ShellWindow : Window
{
    public ShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.SignOutRequested += OnSignOutRequested;
    }

    private void OnSignOutRequested()
    {
        var login = App.Services.GetRequiredService<LoginWindow>();
        App.Current.MainWindow = login;
        login.Show();
        Close();
    }
}
