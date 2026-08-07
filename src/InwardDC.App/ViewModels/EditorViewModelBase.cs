using CommunityToolkit.Mvvm.Input;
using InwardDC.Application.Common;

namespace InwardDC.App.ViewModels;

/// <summary>Base for modal editor view models: close signal + cancel.</summary>
public abstract partial class EditorViewModelBase : ViewModelBase
{
    protected EditorViewModelBase(ICurrentUserService currentUser) : base(currentUser)
    {
    }

    public event Action<bool>? RequestClose;

    protected void NotifyClose(bool saved) => RequestClose?.Invoke(saved);

    [RelayCommand]
    protected void Cancel() => NotifyClose(false);
}
