using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Exceptions;

namespace InwardDC.App.ViewModels;

public partial class CustomerEditorViewModel : EditorViewModelBase
{
    private readonly ICustomerService _customers;
    private Guid? _id;

    public CustomerEditorViewModel(ICurrentUserService currentUser, ICustomerService customers)
        : base(currentUser)
    {
        _customers = customers;
        Title = "Customer";
    }

    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _contactPerson = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _mobile = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _addressLine1 = string.Empty;
    [ObservableProperty] private string _addressLine2 = string.Empty;
    [ObservableProperty] private string _city = string.Empty;
    [ObservableProperty] private string _state = string.Empty;
    [ObservableProperty] private string _pincode = string.Empty;
    [ObservableProperty] private string _country = "India";
    [ObservableProperty] private string _gstin = string.Empty;
    [ObservableProperty] private string _pan = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private bool _isActive = true;
    [ObservableProperty] private string _saveText = "Create";

    public async Task InitializeAsync(Guid? id)
    {
        _id = id;

        if (id.HasValue)
        {
            var dto = await _customers.GetByIdAsync(id.Value);
            if (dto is null)
            {
                ShowError(new NotFoundException("Customer not found."));
                return;
            }

            Code = dto.Code;
            Name = dto.Name;
            ContactPerson = dto.ContactPerson;
            Phone = dto.Phone;
            Mobile = dto.Mobile;
            Email = dto.Email;
            AddressLine1 = dto.AddressLine1;
            AddressLine2 = dto.AddressLine2;
            City = dto.City;
            State = dto.State;
            Pincode = dto.Pincode;
            Country = dto.Country;
            Gstin = dto.GSTIN;
            Pan = dto.PAN;
            Notes = dto.Notes;
            IsActive = dto.IsActive;
            SaveText = "Update";
        }
        else
        {
            Code = await _customers.GenerateCodeAsync();
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await RunAsync(async () =>
        {
            var request = new CustomerSaveRequest
            {
                Id = _id,
                Code = Code,
                Name = Name,
                ContactPerson = ContactPerson,
                Phone = Phone,
                Mobile = Mobile,
                Email = Email,
                AddressLine1 = AddressLine1,
                AddressLine2 = AddressLine2,
                City = City,
                State = State,
                Pincode = Pincode,
                Country = Country,
                GSTIN = Gstin,
                PAN = Pan,
                Notes = Notes,
                IsActive = IsActive
            };

            var result = await _customers.SaveAsync(request);
            if (result.Success)
            {
                SetSuccess(result.Message);
                NotifyClose(true);
            }
            else
            {
                ShowError(new DomainException(result.Message));
            }
        }, "Saving customer...");
    }
}
