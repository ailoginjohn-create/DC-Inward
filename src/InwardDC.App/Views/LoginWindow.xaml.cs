using System.Windows;
using System.Windows.Controls;
using InwardDC.App.ViewModels;
using InwardDC.Application.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace InwardDC.App.Views;

public partial class LoginWindow : Window
{
    private LoginViewModel? _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.LoginSucceeded += OnLoginSucceeded;
        Closed += (_, _) => viewModel.LoginSucceeded -= OnLoginSucceeded;
    }

    private void OnLoginSucceeded(LoginResult result)
    {
        var shell = App.Services.GetRequiredService<ShellWindow>();
        var mainWindow = App.Current.MainWindow;
        App.Current.MainWindow = shell;
        shell.Show();

        if (mainWindow is not null && mainWindow != this)
            mainWindow.Close();

        Close();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.SetPassword(((PasswordBox)sender).Password);
    }
}
