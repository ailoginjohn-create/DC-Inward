using Xunit;
using InwardDC.Application.DTOs;
using InwardDC.Application.Services;
using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;

namespace InwardDC.Tests;

public class RepositoryAndSettingsTests
{
    [Fact]
    public async Task CustomerRepository_Paging_ReturnsCorrectPageAndCount()
    {
        using var app = new TestApp();

        for (int i = 1; i <= 25; i++)
        {
            await app.Uow.Customers.AddAsync(new Customer
            {
                Code = $"CUS-{i:000}",
                Name = $"Customer {i:000}",
                IsActive = true
            });
        }
        await app.Uow.SaveChangesAsync();

        var page1 = await app.Uow.Customers.GetPagedAsync(new CustomerSearchFilter
        {
            Page = 1,
            PageSize = 10,
            SortBy = "code",
            SearchText = "Customer"
        });

        Assert.Equal(25, page1.TotalCount);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal("CUS-001", page1.Items[0].Code);

        var page3 = await app.Uow.Customers.GetPagedAsync(new CustomerSearchFilter
        {
            Page = 3,
            PageSize = 10,
            SortBy = "code"
        });
        Assert.Equal(5, page3.Items.Count);
    }

    [Fact]
    public async Task CustomerRepository_SoftDeletedRecords_AreExcluded()
    {
        using var app = new TestApp();

        var keep = new Customer { Code = "CUS-K", Name = "Keep Me", IsActive = true };
        var gone = new Customer { Code = "CUS-G", Name = "Delete Me", IsActive = true };
        await app.Uow.Customers.AddAsync(keep);
        await app.Uow.Customers.AddAsync(gone);
        await app.Uow.SaveChangesAsync();

        gone.IsDeleted = true;
        gone.DeletedOn = DateTime.UtcNow;
        app.Uow.Customers.Update(gone);
        await app.Uow.SaveChangesAsync();

        var page = await app.Uow.Customers.GetPagedAsync(new CustomerSearchFilter { PageSize = 50 });
        Assert.Equal(1, page.TotalCount);
        Assert.Equal("CUS-K", page.Items[0].Code);
    }

    [Fact]
    public async Task SettingsService_RoundTripsCompanySettings()
    {
        using var app = new TestApp();
        var service = new SettingsService(app.Uow, app.CurrentUser);

        var initial = await service.GetCompanySettingsAsync();
        Assert.Equal("My Company", initial.CompanyName);
        Assert.Equal("INW", initial.InwardNumberPrefix);
        Assert.Equal("DC", initial.DcNumberPrefix);
        Assert.True(initial.RequireSerialForTrackedItems);

        var updated = new CompanySettingsDto
        {
            CompanyName = "City Medtech Pvt Ltd",
            InwardNumberPrefix = "IM",
            DcNumberPrefix = "OUT",
            RequireSerialForTrackedItems = false
        };
        var save = await service.SaveCompanySettingsAsync(updated);
        Assert.True(save.Success);

        var reloaded = await service.GetCompanySettingsAsync();
        Assert.Equal("City Medtech Pvt Ltd", reloaded.CompanyName);
        Assert.Equal("IM", reloaded.InwardNumberPrefix);
        Assert.False(reloaded.RequireSerialForTrackedItems);
    }

    [Fact]
    public async Task SequenceRepository_IsAtomicAcrossIncrements()
    {
        using var app = new TestApp();

        var n1 = await app.Uow.Sequences.GetNextAsync("TestEntity", "TST", 2026);
        var n2 = await app.Uow.Sequences.GetNextAsync("TestEntity", "TST", 2026);
        var n3 = await app.Uow.Sequences.GetNextAsync("TestEntity", "TST", 2026);

        Assert.Equal(1, n1);
        Assert.Equal(2, n2);
        Assert.Equal(3, n3);

        var current = await app.Uow.Sequences.GetCurrentAsync("TestEntity", "TST", 2026);
        Assert.NotNull(current);
        Assert.Equal(3, current!.LastNumber);
    }

    [Fact]
    public async Task AuditService_RecordsCurrentUserOnEveryCall()
    {
        using var app = new TestApp();
        var audit = new AuditService(app.Uow, app.CurrentUser);

        await audit.AddAsync(AuditAction.Create, "Customer", Guid.NewGuid(), "Created a test customer.");
        await audit.AddAsync(AuditAction.Update, "Customer", Guid.NewGuid(), "Updated a test customer.");

        var logs = await audit.GetPagedAsync(new AuditLogFilter { PageSize = 50 });
        Assert.Equal(2, logs.TotalCount);
        Assert.All(logs.Items, l =>
        {
            Assert.Equal("admin", l.UserName);
            Assert.NotNull(l.EntityId);
        });
    }
}
