using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.App.Services;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;

namespace InwardDC.App.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly IDialogService _dialogs;

    public LoginViewModel(ICurrentUserService currentUser, IAuthService auth, IDialogService dialogs)
        : base(currentUser)
    {
        _auth = auth;
        _dialogs = dialogs;
        Title = "Sign In";
    }

    [ObservableProperty]
    private string _userName = "admin";

    [ObservableProperty]
    private string _password = string.Empty;

    public void SetPassword(string password) => Password = password;

    public event Action<LoginResult>? LoginSucceeded;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrEmpty(Password))
        {
            HasError = true;
            ErrorMessage = "Enter your user name and password.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Signing in...";
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _auth.LoginAsync(new LoginRequest { UserName = UserName.Trim(), Password = Password });
            if (result.Success && result.User is not null)
            {
                SetSuccess($"Welcome, {result.User.FullName}.");
                LoginSucceeded?.Invoke(result);
            }
            else
            {
                HasError = true;
                ErrorMessage = result.Message;
                StatusMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Shutdown() => System.Windows.Application.Current.Shutdown();
}
