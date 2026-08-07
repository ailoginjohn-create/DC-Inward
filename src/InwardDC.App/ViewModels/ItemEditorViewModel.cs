using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Exceptions;

namespace InwardDC.App.ViewModels;

public partial class ItemEditorViewModel : EditorViewModelBase
{
    private readonly IItemService _items;
    private readonly IItemCategoryService _categories;
    private Guid? _id;

    public ItemEditorViewModel(ICurrentUserService currentUser, IItemService items,
        IItemCategoryService categories) : base(currentUser)
    {
        _items = items;
        _categories = categories;
        Title = "Item";
    }

    public ObservableCollection<DropdownItemDto> Categories { get; } = new();

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private DropdownItemDto? _category;
    [ObservableProperty] private string _make = string.Empty;
    [ObservableProperty] private string _model = string.Empty;
    [ObservableProperty] private string _unit = "Nos";
    [ObservableProperty] private string _hsnCode = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private bool _isSerialTracked;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _saveText = "Create";

    public async Task InitializeAsync(Guid? id)
    {
        _id = id;

        await RunAsync(async () =>
        {
            Categories.Clear();
            foreach (var c in await _categories.GetDropdownAsync())
                Categories.Add(c);

            if (id.HasValue)
            {
                var dto = await _items.GetByIdAsync(id.Value);
                if (dto is null)
                {
                    ShowError(new NotFoundException("Item not found."));
                    return;
                }

                Code = dto.Code;
                Name = dto.Name;
                Category = Categories.FirstOrDefault(c => c.Id == dto.CategoryId);
                Make = dto.Make;
                Model = dto.Model;
                Unit = dto.Unit;
                HsnCode = dto.HsnCode;
                Description = dto.Description;
                IsSerialTracked = dto.IsSerialTracked;
                IsActive = dto.IsActive;
                SaveText = "Update";
            }
            else
            {
                Code = await _items.GenerateCodeAsync();
            }
        }, "Loading item...");
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await RunAsync(async () =>
        {
            var result = await _items.SaveAsync(new ItemSaveRequest
            {
                Id = _id,
                Code = Code,
                Name = Name,
                CategoryId = Category?.Id,
                Make = Make,
                Model = Model,
                Unit = Unit,
                HsnCode = HsnCode,
                Description = Description,
                IsSerialTracked = IsSerialTracked,
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
        }, "Saving item...");
    }
}
