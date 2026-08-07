using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Exceptions;

namespace InwardDC.App.ViewModels;

public partial class ItemCategoryEditorViewModel : EditorViewModelBase
{
    private readonly IItemCategoryService _categories;
    private Guid? _id;

    public ItemCategoryEditorViewModel(ICurrentUserService currentUser, IItemCategoryService categories)
        : base(currentUser)
    {
        _categories = categories;
        Title = "Item Category";
    }

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _saveText = "Create";

    public async Task InitializeAsync(Guid? id)
    {
        _id = id;

        if (id.HasValue)
        {
            var dto = await _categories.GetByIdAsync(id.Value);
            if (dto is null)
            {
                ShowError(new NotFoundException("Category not found."));
                return;
            }

            Code = dto.Code;
            Name = dto.Name;
            Description = dto.Description;
            IsActive = dto.IsActive;
            SaveText = "Update";
        }
        else
        {
            Code = await _categories.GenerateCodeAsync();
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await RunAsync(async () =>
        {
            var result = await _categories.SaveAsync(new ItemCategorySaveRequest
            {
                Id = _id,
                Code = Code,
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
        }, "Saving category...");
    }
}
