using CommunityToolkit.Mvvm.ComponentModel;
using InwardDC.Application.Common;
using InwardDC.Domain.Exceptions;

namespace InwardDC.App.ViewModels;

/// <summary>
/// Base for all view models: busy state, status/error surfacing, admin flag and a
/// uniform exception-to-message mapping used by every screen.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    protected readonly ICurrentUserService CurrentUser;

    protected ViewModelBase(ICurrentUserService currentUser)
    {
        CurrentUser = currentUser;
    }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    protected bool IsAdmin => CurrentUser.IsAdmin;

    /// <summary>Invoked by the shell whenever the module becomes active.</summary>
    public virtual Task OnNavigatedAsync(CancellationToken ct = default) => Task.CompletedTask;

    protected async Task RunAsync(Func<Task> action, string busyText = "Working...", CancellationToken ct = default)
    {
        IsBusy = true;
        StatusMessage = busyText;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            IsBusy = false;
            if (!HasError)
                StatusMessage = "Ready";
        }
    }

    protected void ShowError(Exception ex)
    {
        HasError = true;
        ErrorMessage = ToFriendlyMessage(ex);
        StatusMessage = ErrorMessage;
    }

    protected void SetSuccess(string message)
    {
        HasError = false;
        ErrorMessage = string.Empty;
        StatusMessage = message;
    }

    private static string ToFriendlyMessage(Exception ex)
    {
        if (ex is OperationCanceledException)
            return "The operation was cancelled.";
        if (ex is DomainException domain)
            return domain.Message;
        if (ex is AggregateException aggregate)
        {
            var inner = aggregate.GetBaseException();
            if (inner is DomainException d)
                return d.Message;
            return inner.Message;
        }
        return ex.Message;
    }

    protected static string FriendlyMessage(Exception ex) => ToFriendlyMessage(ex);
}
