using System.Windows;
using InwardDC.App.Services;
using InwardDC.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace InwardDC.App.Views;

public partial class ShellWindow : Window
{
    private readonly ShellViewModel _viewModel;

    public ShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.SignOutRequested += OnSignOutRequested;
        Loaded += OnLoaded;
    }

    private void OnSignOutRequested()
    {
        var login = App.Services.GetRequiredService<LoginWindow>();
        App.Current.MainWindow = login;
        login.Show();
        Close();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        // Rebuild nav modules in case a different user signed in since the shell
        // ViewModel was first created (the ViewModel is a singleton).
        _viewModel.RefreshNavItems();

        UpdateInfo? info = null;
        try
        {
            var updates = App.Services.GetRequiredService<IUpdateService>();
            info = await updates.CheckForUpdateAsync();
        }
        catch
        {
            return;
        }

        if (info is null) return;

        var dialogs = App.Services.GetRequiredService<IDialogService>();
        var message = $"A new version ({info.Version}) is available." +
                      (string.IsNullOrEmpty(info.Notes) ? string.Empty : $"\n\n{info.Notes}") +
                      "\n\nDownload and install now?";
        if (!dialogs.Confirm(message, "Update Available"))
            return;

        try
        {
            var updates = App.Services.GetRequiredService<IUpdateService>();
            var path = await updates.DownloadUpdateAsync(info);
            if (updates.ApplyUpdate(path))
                System.Windows.Application.Current.Shutdown();
            else
                dialogs.ShowError("The update was downloaded but could not be applied automatically.", "Update");
        }
        catch (Exception ex)
        {
            dialogs.ShowError("The update could not be downloaded.\n\n" + ex.Message, "Update");
        }
    }
}
