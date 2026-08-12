using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Exceptions;

namespace InwardDC.App.ViewModels;

public partial class PurposeEditorViewModel : EditorViewModelBase
{
    private readonly IPurposeService _purposes;
    private Guid? _id;

    public PurposeEditorViewModel(ICurrentUserService currentUser, IPurposeService purposes)
        : base(currentUser)
    {
        _purposes = purposes;
        Title = "Purpose";
    }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _saveText = "Create";

    public async Task InitializeAsync(Guid? id)
    {
        _id = id;

        if (id.HasValue)
        {
            var dto = await _purposes.GetByIdAsync(id.Value);
            if (dto is null)
            {
                ShowError(new NotFoundException("Purpose not found."));
                return;
            }

            Name = dto.Name;
            Description = dto.Description;
            IsActive = dto.IsActive;
            SaveText = "Update";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await RunAsync(async () =>
        {
            var result = await _purposes.SaveAsync(new PurposeSaveRequest
            {
                Id = _id,
                Name = Name,
                Description = Description,
                IsActive = IsActive
            });

            if (result.Success)
            {
                SetSuccess(result.Message);
                NotifyClose(true);
            }
            else
            {
                ShowError(new DomainException(result.Message));
            }
        }, "Saving purpose...");
    }
}
