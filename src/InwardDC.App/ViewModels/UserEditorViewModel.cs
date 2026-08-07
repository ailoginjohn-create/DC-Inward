using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Exceptions;

namespace InwardDC.App.ViewModels;

public partial class UserEditorViewModel : EditorViewModelBase
{
    private readonly IUserService _users;
    private Guid? _id;

    public UserEditorViewModel(ICurrentUserService currentUser, IUserService users)
        : base(currentUser)
    {
        _users = users;
        Title = "User";
    }

    public IReadOnlyList<UserRole> Roles => Enum.GetValues<UserRole>();

    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private UserRole _role = UserRole.User;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private bool _isNew = true;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;
    [ObservableProperty] private string _saveText = "Create";

    public void SetPassword(string value) => Password = value;
    public void SetConfirmPassword(string value) => ConfirmPassword = value;

    public async Task InitializeAsync(Guid? id)
    {
        _id = id;

        if (id.HasValue)
        {
            var dto = await _users.GetByIdAsync(id.Value);
            if (dto is null)
            {
                ShowError(new NotFoundException("User not found."));
                return;
            }

            IsNew = false;
            UserName = dto.UserName;
            FullName = dto.FullName;
            Email = dto.Email;
            Phone = dto.Phone;
            Role = dto.Role;
            IsActive = dto.IsActive;
            SaveText = "Update";
        }
        else
        {
            IsNew = true;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await RunAsync(async () =>
        {
            OperationResult result;

            if (IsNew)
            {
                if (string.IsNullOrWhiteSpace(Password) || Password != ConfirmPassword)
                {
                    ShowError(new ValidationException("Password and confirmation must match and cannot be empty."));
                    return;
                }

                result = await _users.CreateAsync(new CreateUserRequest
                {
                    UserName = UserName,
                    FullName = FullName,
                    Email = Email,
                    Phone = Phone,
                    Role = Role,
                    Password = Password,
                    ConfirmPassword = ConfirmPassword
                });
            }
            else
            {
                result = await _users.UpdateAsync(new UpdateUserRequest
                {
                    Id = _id!.Value,
                    FullName = FullName,
                    Email = Email,
                    Phone = Phone,
                    Role = Role,
                    IsActive = IsActive
                });
            }

            if (result.Success)
            {
                SetSuccess(result.Message);
                NotifyClose(true);
            }
            else
            {
                ShowError(new DomainException(result.Message));
            }
        }, "Saving user...");
    }
}
