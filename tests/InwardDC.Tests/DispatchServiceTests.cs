using Xunit;
using InwardDC.Application.DTOs;
using InwardDC.Application.Services;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace InwardDC.Tests;

public class DispatchServiceTests
{
    private static (InwardService Inward, DispatchService Dispatch) CreateServices(TestApp app)
    {
        var audit = new AuditService(app.Uow, app.CurrentUser);
        var settings = new SettingsService(app.Uow, app.CurrentUser);
        return (
            new InwardService(app.Uow, app.CurrentUser, audit, settings, NullLogger<InwardService>.Instance),
            new DispatchService(app.Uow, app.CurrentUser, audit, settings, NullLogger<DispatchService>.Instance)
        );
    }

    private static Guid ExtractId(object? data)
        => Assert.IsType<Guid>(data!.GetType().GetProperty("Id")!.GetValue(data));

    private async Task<(Guid inwardId, Guid inwardItemId, string serialNo, TestApp app)> SeedSerialInwardAsync()
    {
        var app = new TestApp();
        var customerId = await app.AddCustomerAsync("Dispatch Buyer");
        var itemId = await app.AddSerialTrackedItemAsync("ITM-1000", "Infusion Pump");
        var (inward, _) = CreateServices(app);

        var result = await inward.SaveAsync(new InwardSaveRequest
        {
            InwardType = InwardType.CustomerReturn,
            CustomerId = customerId,
            Items =
            {
                new InwardItemLineRequest
                {
                    ItemId = itemId,
                    ItemName = "Infusion Pump",
                    Quantity = 1,
                    Rate = 1200,
                    Amount = 1200,
                    Serials = { "IP-9001" }
                }
            }
        });

        var inwardId = ExtractId(result.Data);
        var dto = await inward.GetByIdAsync(inwardId);
        var inwardItemId = dto!.Items[0].Id;

        return (inwardId, inwardItemId, "IP-9001", app);
    }

    [Fact]
    public async Task SaveAsync_FullDispatch_MarksSerialsDispatchedAndInwardFullyDispatched()
    {
        var (inwardId, inwardItemId, serialNo, app) = await SeedSerialInwardAsync();
        using (app)
        {
            var (_, dispatch) = CreateServices(app);
            var customerId = await app.Uow.Inwards.GetByIdAsync(inwardId).ContinueWith(t => t.Result!.CustomerId!.Value);

            var result = await dispatch.SaveAsync(new DispatchSaveRequest
            {
                CustomerId = customerId,
                SourceInwardEntryId = inwardId,
                Items =
                {
                    new DispatchLineRequest
                    {
                        SourceInwardItemId = inwardItemId,
                        ItemName = "Infusion Pump",
                        Quantity = 1,
                        Rate = 1200,
                        Amount = 1200,
                        Serials = { serialNo }
                    }
                }
            });

            Assert.True(result.Success);

            var dcId = ExtractId(result.Data);
            var dc = await dispatch.GetByIdAsync(dcId);
            Assert.NotNull(dc);
            Assert.Equal(DispatchStatus.Generated, dc!.Status);
            Assert.Contains(serialNo, dc.Items[0].Serials);

            // Serial moved to Dispatched.
            var serial = await app.Uow.SerialNumbers.GetBySerialAsync(serialNo);
            Assert.NotNull(serial);
            Assert.Equal(SerialStatus.Dispatched, serial!.Status);
            Assert.Equal(dcId, serial.DispatchChallanId);

            // Inward fully dispatched.
            var inward = await app.Uow.Inwards.GetByIdAsync(inwardId);
            Assert.Equal(InwardStatus.FullyDispatched, inward!.Status);
            Assert.Equal(1, inward.Items.Single().DispatchedQuantity);

            // Available stock is now empty.
            var stock = await dispatch.GetAvailableStockAsync();
            Assert.Empty(stock);
        }
    }

    [Fact]
    public async Task SaveAsync_OverDispatch_ThrowsBusinessRule()
    {
        var (inwardId, inwardItemId, serialNo, app) = await SeedSerialInwardAsync();
        using (app)
        {
            var (_, dispatch) = CreateServices(app);
            var customerId = await app.Uow.Inwards.GetByIdAsync(inwardId).ContinueWith(t => t.Result!.CustomerId!.Value);

            await Assert.ThrowsAsync<BusinessRuleException>(() => dispatch.SaveAsync(new DispatchSaveRequest
            {
                CustomerId = customerId,
                Items =
                {
                    new DispatchLineRequest
                    {
                        SourceInwardItemId = inwardItemId,
                        ItemName = "Infusion Pump",
                        Quantity = 2,  // only 1 in stock
                        Rate = 1200,
                        Amount = 2400,
                        Serials = { serialNo, "IP-9002" }
                    }
                }
            }));
        }
    }

    [Fact]
    public async Task SaveAsync_UnknownSerial_ThrowsValidation()
    {
        var (inwardId, inwardItemId, _, app) = await SeedSerialInwardAsync();
        using (app)
        {
            var (_, dispatch) = CreateServices(app);
            var customerId = await app.Uow.Inwards.GetByIdAsync(inwardId).ContinueWith(t => t.Result!.CustomerId!.Value);

            await Assert.ThrowsAsync<ValidationException>(() => dispatch.SaveAsync(new DispatchSaveRequest
            {
                CustomerId = customerId,
                Items =
                {
                    new DispatchLineRequest
                    {
                        SourceInwardItemId = inwardItemId,
                        ItemName = "Infusion Pump",
                        Quantity = 1,
                        Rate = 1200,
                        Amount = 1200,
                        Serials = { "NOT-OWNED-SERIAL" }
                    }
                }
            }));
        }
    }

    [Fact]
    public async Task CancelAsync_ReversesStockAndSerials()
    {
        var (inwardId, inwardItemId, serialNo, app) = await SeedSerialInwardAsync();
        using (app)
        {
            var (_, dispatch) = CreateServices(app);
            var customerId = await app.Uow.Inwards.GetByIdAsync(inwardId).ContinueWith(t => t.Result!.CustomerId!.Value);

            var result = await dispatch.SaveAsync(new DispatchSaveRequest
            {
                CustomerId = customerId,
                Items =
                {
                    new DispatchLineRequest
                    {
                        SourceInwardItemId = inwardItemId,
                        ItemName = "Infusion Pump",
                        Quantity = 1,
                        Rate = 1200,
                        Amount = 1200,
                        Serials = { serialNo }
                    }
                }
            });
            var dcId = ExtractId(result.Data);

            var cancel = await dispatch.CancelAsync(dcId);
            Assert.True(cancel.Success);

            var serial = await app.Uow.SerialNumbers.GetBySerialAsync(serialNo);
            Assert.NotNull(serial);
            Assert.Equal(SerialStatus.InStock, serial!.Status);
            Assert.Null(serial.DispatchChallanId);
            Assert.Null(serial.DispatchItemId);

            var inward = await app.Uow.Inwards.GetByIdAsync(inwardId);
            Assert.Equal(InwardStatus.Received, inward!.Status);
            Assert.Equal(0, inward.Items.Single().DispatchedQuantity);

            var dc = await dispatch.GetByIdAsync(dcId);
            Assert.Equal(DispatchStatus.Cancelled, dc!.Status);

            // Stock available again.
            var stock = await dispatch.GetAvailableStockAsync();
            Assert.Single(stock);
            Assert.Equal(1, stock[0].AvailableQuantity);
        }
    }

    [Fact]
    public async Task SaveAsync_PartialDispatch_SetsPartiallyDispatched()
    {
        using var app = new TestApp();
        var customerId = await app.AddCustomerAsync("Buyer Two");
        var itemId = await app.AddPlainItemAsync("ITM-2000", "Oxygen Cylinder");
        var (inward, dispatch) = CreateServices(app);

        var inwardResult = await inward.SaveAsync(new InwardSaveRequest
        {
            InwardType = InwardType.CustomerReturn,
            CustomerId = customerId,
            Items =
            {
                new InwardItemLineRequest
                {
                    ItemId = itemId,
                    ItemName = "Oxygen Cylinder",
                    Quantity = 10,
                    Rate = 100,
                    Amount = 1000
                }
            }
        });
        var inwardId = ExtractId(inwardResult.Data);
        var inwardItemId = (await inward.GetByIdAsync(inwardId))!.Items[0].Id;

        var dcResult = await dispatch.SaveAsync(new DispatchSaveRequest
        {
            CustomerId = customerId,
            SourceInwardEntryId = inwardId,
            Items =
            {
                new DispatchLineRequest
                {
                    SourceInwardItemId = inwardItemId,
                    ItemName = "Oxygen Cylinder",
                    Quantity = 4,
                    Rate = 100,
                    Amount = 400
                }
            }
        });
        Assert.True(dcResult.Success);

        var entry = await app.Uow.Inwards.GetByIdAsync(inwardId);
        Assert.Equal(InwardStatus.PartiallyDispatched, entry!.Status);
        Assert.Equal(4, entry.Items.Single().DispatchedQuantity);

        var stock = await dispatch.GetAvailableStockAsync();
        Assert.Single(stock);
        Assert.Equal(6, stock[0].AvailableQuantity);
    }
}
