namespace InwardDC.App.Services;

using System.Windows;

/// <summary>Lightweight message / file-picker / dialog host abstraction.</summary>
public interface IDialogService
{
    void ShowInfo(string message, string title = "Inward & DC");
    void ShowWarning(string message, string title = "Inward & DC");
    void ShowError(string message, string title = "Inward & DC");
    bool Confirm(string message, string title = "Confirm");
    string? PickOpenFile(string filter, string? initialDirectory = null);
    string? PickSaveFile(string filter, string defaultFileName, string? initialDirectory = null);
    string? PickFolder(string? initialDirectory = null);
    bool ShowDialog(Window window);
}
