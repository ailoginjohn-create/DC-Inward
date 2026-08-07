using System.Windows;
using InwardDC.App.Views;
using Microsoft.Win32;

namespace InwardDC.App.Services;

public sealed class DialogService : IDialogService
{
    public void ShowInfo(string message, string title = "Inward & DC")
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void ShowWarning(string message, string title = "Inward & DC")
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public void ShowError(string message, string title = "Inward & DC")
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public bool Confirm(string message, string title = "Confirm")
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public string? PickOpenFile(string filter, string? initialDirectory = null)
    {
        var dlg = new OpenFileDialog { Filter = filter, CheckFileExists = true };
        if (!string.IsNullOrWhiteSpace(initialDirectory))
            dlg.InitialDirectory = initialDirectory;
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? PickSaveFile(string filter, string defaultFileName, string? initialDirectory = null)
    {
        var dlg = new SaveFileDialog { Filter = filter, FileName = defaultFileName, AddExtension = true };
        if (!string.IsNullOrWhiteSpace(initialDirectory))
            dlg.InitialDirectory = initialDirectory;
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? PickFolder(string? initialDirectory = null)
    {
        var owner = System.Windows.Application.Current?.MainWindow;
        var dlg = new FolderPickerWindow(initialDirectory) { Owner = owner };
        return dlg.ShowDialog() == true ? dlg.SelectedPath : null;
    }

    public bool ShowDialog(Window window) => window.ShowDialog() == true;
}
