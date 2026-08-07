using Xunit;
using InwardDC.Application.DTOs;
using InwardDC.Application.Services;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace InwardDC.Tests;

public class InwardServiceTests
{
    private static InwardService CreateService(TestApp app)
        => new(app.Uow, app.CurrentUser, new AuditService(app.Uow, app.CurrentUser),
            new SettingsService(app.Uow, app.CurrentUser), NullLogger<InwardService>.Instance);

    [Fact]
    public async Task SaveAsync_CreatesInward_ForNonSerialItem()
    {
        using var app = new TestApp();
        var customerId = await app.AddCustomerAsync("City Hospital");
        var itemId = await app.AddPlainItemAsync("ITM-0001", "Surgical Gloves");
        var service = CreateService(app);

        var result = await service.SaveAsync(new InwardSaveRequest
        {
            InwardType = InwardType.CustomerReturn,
            CustomerId = customerId,
            ReferenceInvoiceNo = "INV-001",
            Items =
            {
                new InwardItemLineRequest
                {
                    ItemId = itemId,
                    ItemName = "Surgical Gloves",
                    Quantity = 10,
                    Rate = 5,
                    Amount = 50
                }
            }
        });

        Assert.True(result.Success);
        var id = Assert.IsType<Guid>(result.Data!.GetType().GetProperty("Id")!.GetValue(result.Data));

        var saved = await service.GetByIdAsync(id);
        Assert.NotNull(saved);
        Assert.StartsWith("INW/", saved!.InwardNo);
        Assert.Equal(10, saved.TotalQuantity);
        Assert.Equal(50, saved.TotalAmount);
        Assert.Equal(InwardStatus.Received, saved.Status);
        Assert.Single(saved.Items);
        Assert.Empty(saved.Items[0].Serials);

        // Audit + item events recorded.
        Assert.True(await app.Uow.AuditLogs.CountAsync() > 0);
    }

    [Fact]
    public async Task SaveAsync_SerialTracked_RequiresSerials()
    {
        using var app = new TestApp();
        var customerId = await app.AddCustomerAsync("City Hospital");
        var itemId = await app.AddSerialTrackedItemAsync("ITM-0002", "Patient Monitor");
        var service = CreateService(app);

        await Assert.ThrowsAsync<ValidationException>(() => service.SaveAsync(new InwardSaveRequest
        {
            InwardType = InwardType.CustomerReturn,
            CustomerId = customerId,
            Items =
            {
                new InwardItemLineRequest
                {
                    ItemId = itemId,
                    ItemName = "Patient Monitor",
                    Quantity = 2,
                    Rate = 1000,
                    Amount = 2000,
                    Serials = { }  // serials required
                }
            }
        }));
    }

    [Fact]
    public async Task SaveAsync_SerialTracked_RejectsCountMismatch()
    {
        using var app = new TestApp();
        var customerId = await app.AddCustomerAsync("City Hospital");
        var itemId = await app.AddSerialTrackedItemAsync("ITM-0003", "Defibrillator");
        var service = CreateService(app);

        await Assert.ThrowsAsync<ValidationException>(() => service.SaveAsync(new InwardSaveRequest
        {
            InwardType = InwardType.CustomerReturn,
            CustomerId = customerId,
            Items =
            {
                new InwardItemLineRequest
                {
                    ItemId = itemId,
                    ItemName = "Defibrillator",
                    Quantity = 2,
                    Rate = 500,
                    Amount = 1000,
                    Serials = { "SN-0001" }  // only 1 serial for qty 2
                }
            }
        }));
    }

    [Fact]
    public async Task SaveAsync_SerialTracked_CreatesSerialsAndEvents()
    {
        using var app = new TestApp();
        var customerId = await app.AddCustomerAsync("City Hospital");
        var itemId = await app.AddSerialTrackedItemAsync("ITM-0004", "Ventilator");
        var service = CreateService(app);

        var result = await service.SaveAsync(new InwardSaveRequest
        {
            InwardType = InwardType.CustomerReturn,
            CustomerId = customerId,
            Items =
            {
                new InwardItemLineRequest
                {
                    ItemId = itemId,
                    ItemName = "Ventilator",
                    Quantity = 2,
                    Rate = 2500,
                    Amount = 5000,
                    Serials = { "VNT-1001", "VNT-1002" }
                }
            }
        });

        Assert.True(result.Success);
        var id = Assert.IsType<Guid>(result.Data!.GetType().GetProperty("Id")!.GetValue(result.Data));
        var saved = await service.GetByIdAsync(id);

        Assert.Equal(2, saved!.Items[0].Serials.Count);
        Assert.Contains("VNT-1001", saved.Items[0].Serials);

        var serials = await app.Uow.SerialNumbers.GetByInwardAsync(id);
        Assert.Equal(2, serials.Count);
        Assert.All(serials, s => Assert.Equal(SerialStatus.InStock, s.Status));

        var events = await app.Uow.ItemEvents.GetByItemAsync(itemId);
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal(ItemEventType.InwardReceived, e.EventType));
    }

    [Fact]
    public async Task SaveAsync_RejectsDuplicateSerial_AcrossInwards()
    {
        using var app = new TestApp();
        var customerId = await app.AddCustomerAsync("City Hospital");
        var itemId = await app.AddSerialTrackedItemAsync("ITM-0005", "MRI Scanner");
        var service = CreateService(app);

        var first = await service.SaveAsync(new InwardSaveRequest
        {
            InwardType = InwardType.CustomerReturn,
            CustomerId = customerId,
            Items =
            {
                new InwardItemLineRequest
                {
                    ItemId = itemId,
                    ItemName = "MRI Scanner",
                    Quantity = 1,
                    Rate = 9000,
                    Amount = 9000,
                    Serials = { "MRI-0001" }
                }
            }
        });
        Assert.True(first.Success);

        await Assert.ThrowsAsync<DuplicateException>(() => service.SaveAsync(new InwardSaveRequest
        {
            InwardType = InwardType.CustomerReturn,
            CustomerId = customerId,
            Items =
            {
                new InwardItemLineRequest
                {
                    ItemId = itemId,
                    ItemName = "MRI Scanner",
                    Quantity = 1,
                    Rate = 9000,
                    Amount = 9000,
                    Serials = { "MRI-0001" }
                }
            }
        }));
    }

    [Fact]
    public async Task SaveAsync_RejectsCustomerReturnWithoutCustomer()
    {
        using var app = new TestApp();
        var service = CreateService(app);

        await Assert.ThrowsAsync<ValidationException>(() => service.SaveAsync(new InwardSaveRequest
        {
            InwardType = InwardType.CustomerReturn,
            Items =
            {
                new InwardItemLineRequest { ItemName = "X", Quantity = 1, Rate = 1, Amount = 1 }
            }
        }));
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesEntryAndSerials()
    {
        using var app = new TestApp();
        var customerId = await app.AddCustomerAsync("City Hospital");
        var itemId = await app.AddSerialTrackedItemAsync("ITM-0006", "ECG Machine");
        var service = CreateService(app);

        var result = await service.SaveAsync(new InwardSaveRequest
        {
            InwardType = InwardType.CustomerReturn,
            CustomerId = customerId,
            Items =
            {
                new InwardItemLineRequest
                {
                    ItemId = itemId,
                    ItemName = "ECG Machine",
                    Quantity = 1,
                    Rate = 800,
                    Amount = 800,
                    Serials = { "ECG-77" }
                }
            }
        });
        var id = Assert.IsType<Guid>(result.Data!.GetType().GetProperty("Id")!.GetValue(result.Data));

        var del = await service.DeleteAsync(id);
        Assert.True(del.Success);

        Assert.Null(await service.GetByIdAsync(id));
        var serials = await app.Uow.SerialNumbers.GetByInwardAsync(id);
        Assert.Empty(serials);  // soft-deleted serials are excluded
    }
}
