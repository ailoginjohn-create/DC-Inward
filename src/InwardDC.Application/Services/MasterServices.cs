using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Exceptions;
using InwardDC.Domain.Interfaces;

namespace InwardDC.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly ISettingsService _settings;

    public CustomerService(IUnitOfWork uow, ICurrentUserService currentUser, IAuditService audit, ISettingsService settings)
    {
        _uow = uow;
        _currentUser = currentUser;
        _audit = audit;
        _settings = settings;
    }

    public async Task<PagedResponse<CustomerDto>> GetPagedAsync(CustomerSearchFilter filter, CancellationToken ct = default)
    {
        var result = await _uow.Customers.GetPagedAsync(filter, ct);
        return ToPage(result);
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _uow.Customers.GetByIdAsync(id, ct);
        return c is null ? null : ToDto(c);
    }

    public async Task<IReadOnlyList<DropdownItemDto>> GetDropdownAsync(CancellationToken ct = default)
    {
        var customers = await _uow.Customers.GetAllActiveAsync(ct);
        return customers.Select(c => new DropdownItemDto(c.Id, c.Code, c.Name, $"{c.City}")).ToList();
    }

    public async Task<OperationResult> SaveAsync(CustomerSaveRequest request, CancellationToken ct = default)
    {
        Validate(request);

        Customer? customer;
        if (request.Id.HasValue)
        {
            customer = await _uow.Customers.GetByIdAsync(request.Id.Value, ct);
            if (customer is null || customer.IsDeleted)
                throw new NotFoundException("Customer not found.");
            customer.ModifiedOn = DateTime.UtcNow;
        }
        else
        {
            if (await _uow.Customers.CodeExistsAsync(request.Code, ct: ct))
                throw new DuplicateException($"Customer code '{request.Code}' already exists.");
            customer = new Customer();
            await _uow.Customers.AddAsync(customer, ct);
        }

        Apply(customer, request);
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(
            request.Id.HasValue ? AuditAction.Update : AuditAction.Create,
            nameof(Customer), customer.Id,
            $"{(request.Id.HasValue ? "Updated" : "Created")} customer '{customer.Code} - {customer.Name}'.",
            ct: ct);

        return OperationResult.Ok(request.Id.HasValue ? "Customer updated." : "Customer created.");
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await _uow.Customers.GetByIdAsync(id, ct);
        if (customer is null || customer.IsDeleted)
            throw new NotFoundException("Customer not found.");

        customer.IsDeleted = true;
        customer.DeletedOn = DateTime.UtcNow;
        customer.DeletedBy = _currentUser.UserId;
        customer.IsActive = false;
        _uow.Customers.Update(customer);
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.Delete, nameof(Customer), id,
            $"Deleted customer '{customer.Code} - {customer.Name}'.", ct: ct);

        return OperationResult.Ok("Customer deleted.");
    }

    public async Task<string> GenerateCodeAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetCompanySettingsAsync(ct);
        var next = await _uow.Sequences.GetNextAsync("Customer", s.CustomerNumberPrefix, DateTime.Today.Year, ct);
        return $"{s.CustomerNumberPrefix}/{DateTime.Today.Year}/{next:0000}";
    }

    private static void Validate(CustomerSaveRequest r)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(r.Name)) errors.Add("Customer name is required.");
        if (string.IsNullOrWhiteSpace(r.Code)) errors.Add("Customer code is required.");
        if (r.Email.Length > 0 && !r.Email.Contains('@')) errors.Add("Email address is not valid.");
        if (errors.Count > 0) throw new ValidationException(errors);
    }

    private static void Apply(Customer c, CustomerSaveRequest r)
    {
        c.Code = r.Code.Trim();
        c.Name = r.Name.Trim();
        c.ContactPerson = r.ContactPerson.Trim();
        c.Phone = r.Phone.Trim();
        c.Mobile = r.Mobile.Trim();
        c.Email = r.Email.Trim();
        c.AddressLine1 = r.AddressLine1.Trim();
        c.AddressLine2 = r.AddressLine2.Trim();
        c.City = r.City.Trim();
        c.State = r.State.Trim();
        c.Pincode = r.Pincode.Trim();
        c.Country = r.Country.Trim();
        c.GSTIN = r.GSTIN.Trim();
        c.PAN = r.PAN.Trim();
        c.Notes = r.Notes.Trim();
        c.IsActive = r.IsActive;
    }

    internal static CustomerDto ToDto(Customer c) => new()
    {
        Id = c.Id,
        Code = c.Code,
        Name = c.Name,
        ContactPerson = c.ContactPerson,
        Phone = c.Phone,
        Mobile = c.Mobile,
        Email = c.Email,
        AddressLine1 = c.AddressLine1,
        AddressLine2 = c.AddressLine2,
        City = c.City,
        State = c.State,
        Pincode = c.Pincode,
        Country = c.Country,
        GSTIN = c.GSTIN,
        PAN = c.PAN,
        Notes = c.Notes,
        IsActive = c.IsActive,
        CreatedOn = c.CreatedOn
    };

    private static PagedResponse<CustomerDto> ToPage(PagedResult<Customer> result)
    {
        var page = new PagedResult<CustomerDto>
        {
            Items = result.Items.Select(ToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
        return PagedResponse<CustomerDto>.From(page);
    }
}

public class VendorService : IVendorService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly ISettingsService _settings;

    public VendorService(IUnitOfWork uow, ICurrentUserService currentUser, IAuditService audit, ISettingsService settings)
    {
        _uow = uow;
        _currentUser = currentUser;
        _audit = audit;
        _settings = settings;
    }

    public async Task<PagedResponse<VendorDto>> GetPagedAsync(VendorSearchFilter filter, CancellationToken ct = default)
    {
        var result = await _uow.Vendors.GetPagedAsync(filter, ct);
        var page = new PagedResult<VendorDto>
        {
            Items = result.Items.Select(ToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
        return PagedResponse<VendorDto>.From(page);
    }

    public async Task<VendorDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var v = await _uow.Vendors.GetByIdAsync(id, ct);
        return v is null ? null : ToDto(v);
    }

    public async Task<IReadOnlyList<DropdownItemDto>> GetDropdownAsync(CancellationToken ct = default)
    {
        var vendors = await _uow.Vendors.GetAllActiveAsync(ct);
        return vendors.Select(v => new DropdownItemDto(v.Id, v.Code, v.Name, $"{v.City}")).ToList();
    }

    public async Task<OperationResult> SaveAsync(VendorSaveRequest request, CancellationToken ct = default)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Name)) errors.Add("Vendor name is required.");
        if (string.IsNullOrWhiteSpace(request.Code)) errors.Add("Vendor code is required.");
        if (errors.Count > 0) throw new ValidationException(errors);

        Vendor? vendor;
        if (request.Id.HasValue)
        {
            vendor = await _uow.Vendors.GetByIdAsync(request.Id.Value, ct);
            if (vendor is null || vendor.IsDeleted)
                throw new NotFoundException("Vendor not found.");
            vendor.ModifiedOn = DateTime.UtcNow;
        }
        else
        {
            if (await _uow.Vendors.CodeExistsAsync(request.Code, ct: ct))
                throw new DuplicateException($"Vendor code '{request.Code}' already exists.");
            vendor = new Vendor();
            await _uow.Vendors.AddAsync(vendor, ct);
        }

        vendor.Code = request.Code.Trim();
        vendor.Name = request.Name.Trim();
        vendor.ContactPerson = request.ContactPerson.Trim();
        vendor.Phone = request.Phone.Trim();
        vendor.Mobile = request.Mobile.Trim();
        vendor.Email = request.Email.Trim();
        vendor.AddressLine1 = request.AddressLine1.Trim();
        vendor.AddressLine2 = request.AddressLine2.Trim();
        vendor.City = request.City.Trim();
        vendor.State = request.State.Trim();
        vendor.Pincode = request.Pincode.Trim();
        vendor.Country = request.Country.Trim();
        vendor.GSTIN = request.GSTIN.Trim();
        vendor.PAN = request.PAN.Trim();
        vendor.Notes = request.Notes.Trim();
        vendor.IsActive = request.IsActive;

        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(
            request.Id.HasValue ? AuditAction.Update : AuditAction.Create,
            nameof(Vendor), vendor.Id,
            $"{(request.Id.HasValue ? "Updated" : "Created")} vendor '{vendor.Code} - {vendor.Name}'.",
            ct: ct);

        return OperationResult.Ok(request.Id.HasValue ? "Vendor updated." : "Vendor created.");
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var vendor = await _uow.Vendors.GetByIdAsync(id, ct);
        if (vendor is null || vendor.IsDeleted)
            throw new NotFoundException("Vendor not found.");

        vendor.IsDeleted = true;
        vendor.DeletedOn = DateTime.UtcNow;
        vendor.DeletedBy = _currentUser.UserId;
        vendor.IsActive = false;
        _uow.Vendors.Update(vendor);
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.Delete, nameof(Vendor), id,
            $"Deleted vendor '{vendor.Code} - {vendor.Name}'.", ct: ct);

        return OperationResult.Ok("Vendor deleted.");
    }

    public async Task<string> GenerateCodeAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetCompanySettingsAsync(ct);
        var next = await _uow.Sequences.GetNextAsync("Vendor", s.VendorNumberPrefix, DateTime.Today.Year, ct);
        return $"{s.VendorNumberPrefix}/{DateTime.Today.Year}/{next:0000}";
    }

    internal static VendorDto ToDto(Vendor v) => new()
    {
        Id = v.Id, Code = v.Code, Name = v.Name, ContactPerson = v.ContactPerson, Phone = v.Phone,
        Mobile = v.Mobile, Email = v.Email, AddressLine1 = v.AddressLine1, AddressLine2 = v.AddressLine2,
        City = v.City, State = v.State, Pincode = v.Pincode, Country = v.Country, GSTIN = v.GSTIN,
        PAN = v.PAN, Notes = v.Notes, IsActive = v.IsActive
    };
}
