using Xunit;
using InwardDC.Application.DTOs;
using InwardDC.Application.Services;
using InwardDC.Domain.Exceptions;

namespace InwardDC.Tests;

public class PurposeServiceTests
{
    private static (PurposeService Service, TestApp App) Create()
    {
        var app = new TestApp();
        var audit = new AuditService(app.Uow, app.CurrentUser);
        return (new PurposeService(app.Uow, app.CurrentUser, audit), app);
    }

    private static async Task<Guid> SaveAsync(PurposeService service, PurposeSaveRequest request)
    {
        var result = await service.SaveAsync(request);
        Assert.True(result.Success);
        var paged = await service.GetPagedAsync(new Domain.Criteria.PurposeSearchFilter
        {
            Page = 1,
            PageSize = 200,
            SearchText = request.Name
        });
        return Assert.Single(paged.Items).Id;
    }

    [Fact]
    public async Task SaveAsync_Create_PersistsAndSeedsDefaults()
    {
        var (service, app) = Create();
        try
        {
            await SaveAsync(service, new PurposeSaveRequest
            {
                Name = "Calibration",
                Description = "Annual calibration",
                IsActive = true
            });

            var paged = await service.GetPagedAsync(new Domain.Criteria.PurposeSearchFilter { Page = 1, PageSize = 50 });
            var purpose = paged.Items.Single(p => p.Name == "Calibration");
            Assert.Equal("Annual calibration", purpose.Description);
            Assert.True(purpose.IsActive);

            var seeded = paged.Items.Select(p => p.Name);
            Assert.Contains("Evaluation", seeded);
            Assert.Contains("Testing", seeded);
            Assert.Contains("Demo", seeded);
            Assert.Contains("Service", seeded);
            Assert.Contains("Other", seeded);
        }
        finally
        {
            app.Dispose();
        }
    }

    [Fact]
    public async Task SaveAsync_DuplicateName_ThrowsDuplicateException()
    {
        var (service, app) = Create();
        try
        {
            await SaveAsync(service, new PurposeSaveRequest { Name = "Rental", IsActive = true });
            await Assert.ThrowsAsync<DuplicateException>(() =>
                service.SaveAsync(new PurposeSaveRequest { Name = "Rental " }));
        }
        finally
        {
            app.Dispose();
        }
    }

    [Fact]
    public async Task SaveAsync_Update_ChangesNameAndSkipsOwnNameDuplicate()
    {
        var (service, app) = Create();
        try
        {
            var id = await SaveAsync(service, new PurposeSaveRequest
            {
                Name = "Repair",
                Description = "Repair work",
                IsActive = true
            });

            var updated = await service.SaveAsync(new PurposeSaveRequest
            {
                Id = id,
                Name = "Repair",
                Description = "Out-of-warranty repair",
                IsActive = false
            });
            Assert.True(updated.Success);

            var dto = await service.GetByIdAsync(id);
            Assert.Equal("Out-of-warranty repair", dto!.Description);
            Assert.False(dto.IsActive);
        }
        finally
        {
            app.Dispose();
        }
    }

    [Fact]
    public async Task DeleteAsync_NotInUse_SoftDeletes()
    {
        var (service, app) = Create();
        try
        {
            var id = await SaveAsync(service, new PurposeSaveRequest { Name = "Loaner", IsActive = true });

            var result = await service.DeleteAsync(id);
            Assert.True(result.Success);

            Assert.Null(await service.GetByIdAsync(id));
        }
        finally
        {
            app.Dispose();
        }
    }

    [Fact]
    public async Task DeleteAsync_InUseByInward_ThrowsBusinessRuleException()
    {
        var app = new TestApp();
        try
        {
            var vendor = new InwardDC.Domain.Entities.Vendor
            {
                Code = $"VEN-{Guid.NewGuid():N}"[..12],
                Name = "Purpose Supplier",
                IsActive = true
            };
            await app.Uow.Vendors.AddAsync(vendor);
            await app.Uow.SaveChangesAsync();
            var itemId = await app.AddPlainItemAsync("ITM-2001", "Spare Part");
            var audit = new AuditService(app.Uow, app.CurrentUser);
            var settings = new SettingsService(app.Uow, app.CurrentUser);
            var inward = new InwardService(app.Uow, app.CurrentUser, audit, settings, Microsoft.Extensions.Logging.Abstractions.NullLogger<InwardService>.Instance);
            var service = new PurposeService(app.Uow, app.CurrentUser, audit);

            var purposeId = await app.Uow.Purposes.GetByNameAsync("Service", ct: default)
                .ContinueWith(t => t.Result!.Id);

            await inward.SaveAsync(new InwardSaveRequest
            {
                InwardType = Domain.Enums.InwardType.Purchase,
                VendorId = vendor.Id,
                PurposeId = purposeId,
                Items =
                {
                    new InwardItemLineRequest
                    {
                        ItemId = itemId,
                        ItemName = "Spare Part",
                        Quantity = 2,
                        Rate = 100,
                        Amount = 200
                    }
                }
            });

            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => service.DeleteAsync(purposeId));
            Assert.Contains("cannot be deleted", ex.Message);
        }
        finally
        {
            app.Dispose();
        }
    }
}
