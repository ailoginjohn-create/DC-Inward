using Xunit;
using ClosedXML.Excel;
using InwardDC.Domain.Entities;
using InwardDC.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace InwardDC.Tests;

public class ExcelImportTests
{
    private static ExcelService CreateService(TestApp app)
        => new(app.Uow, app.CurrentUser, NullLogger<ExcelService>.Instance);

    private static MemoryStream BuildWorkbook(params object[][] rows)
    {
        var stream = new MemoryStream();
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("InwardItems");
        var headers = new[]
        {
            "DATE", "D.C No", "Invoice No", "Items Received From", "Name of Item",
            "Qty", "Serial No", "Purpose", "Remarks", "Received By", "Remarks"
        };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
                ws.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(rows[r][c]);

        wb.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static async Task<(TestApp app, string customerCode)> SeedMastersAsync()
    {
        var app = new TestApp();
        const string customerCode = "CUS-001";
        await app.Uow.Customers.AddAsync(new Customer
        {
            Code = customerCode,
            Name = "Import Hospital",
            IsActive = true
        });
        await app.Uow.Items.AddAsync(new Item
        {
            Code = "ITM-A",
            Name = "Monitor",
            IsSerialTracked = true,
            IsActive = true
        });
        await app.Uow.Items.AddAsync(new Item
        {
            Code = "ITM-B",
            Name = "Gloves",
            IsSerialTracked = false,
            IsActive = true
        });
        await app.Uow.SaveChangesAsync();
        return (app, customerCode);
    }

    [Fact]
    public async Task ImportInwardAsync_CreatesEntryGroupingRowsByHeader()
    {
        var (app, customerCode) = await SeedMastersAsync();
        using (app)
        {
            var service = CreateService(app);
            using var stream = BuildWorkbook(
                new object[] { "01/01/2026", "DC-1", "INV-900", customerCode, "Monitor", 1, "M-0001", "Evaluation", "line remarks", "Rahul", "entry remarks" },
                new object[] { "01/01/2026", "DC-1", "INV-900", customerCode, "Gloves", 5, "", "Evaluation", "", "Rahul", "entry remarks" }
            );

            var result = await service.ImportInwardAsync(stream, "import.xlsx");

            Assert.True(result.Success);
            Assert.Empty(result.Errors);
            Assert.Equal(2, result.ImportedRows);
            Assert.Equal(1, result.CreatedEntries);  // same header -> single inward entry

            var entries = await app.Uow.Inwards.GetByPeriodDetailedAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
            var entry = Assert.Single(entries);
            Assert.StartsWith("INW/", entry.InwardNo);
            Assert.Equal("DC-1", entry.ChallanNo);
            Assert.Equal("Rahul", entry.ReceivedBy);
            Assert.Equal("entry remarks", entry.Remarks);
            Assert.NotNull(entry.Purpose);
            Assert.Equal("Evaluation", entry.Purpose!.Name);
            Assert.Equal(2, entry.Items.Count);
            Assert.Equal(6, entry.TotalQuantity);
            Assert.Equal("line remarks", entry.Items.Single(i => i.ItemName == "Monitor").Remarks);
        }
    }

    [Fact]
    public async Task ImportInwardAsync_SkipsDuplicateSerialWithinFile_AndImportsTheRest()
    {
        var (app, customerCode) = await SeedMastersAsync();
        using (app)
        {
            var service = CreateService(app);
            using var stream = BuildWorkbook(
                new object[] { "01/01/2026", "DC-1", "INV-1", customerCode, "Monitor", 1, "DUP-1", "Evaluation", "", "", "" },
                new object[] { "01/01/2026", "DC-2", "INV-2", customerCode, "Monitor", 1, "DUP-1", "Evaluation", "", "", "" }
            );

            var result = await service.ImportInwardAsync(stream, "import.xlsx");

            Assert.Equal(1, result.CreatedEntries);
            Assert.Equal(1, result.DuplicatesSkipped);
            Assert.Contains(result.Errors, e => e.Message.Contains("Duplicate serial 'DUP-1'"));

            var entries = await app.Uow.Inwards.GetByPeriodDetailedAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
            var entry = Assert.Single(entries);
            Assert.Single(entry.Items);
        }
    }

    [Fact]
    public async Task ImportInwardAsync_ImportsValidRows_WhenOtherRowsFail()
    {
        var (app, customerCode) = await SeedMastersAsync();
        using (app)
        {
            var service = CreateService(app);
            using var stream = BuildWorkbook(
                new object[] { "01/01/2026", "DC-1", "INV-1", customerCode, "Monitor", 1, "OK-1", "Evaluation", "", "", "" },
                new object[] { "01/01/2026", "DC-2", "INV-2", "Nobody Here", "Monitor", 1, "", "Evaluation", "", "", "" },
                new object[] { "01/01/2026", "DC-3", "INV-3", customerCode, "Monitor", 1, "OK-1", "Evaluation", "", "", "" }
            );

            var result = await service.ImportInwardAsync(stream, "import.xlsx");

            Assert.Equal(1, result.ImportedRows);
            Assert.Equal(1, result.CreatedEntries);
            Assert.Equal(1, result.DuplicatesSkipped);
            Assert.Equal(2, result.Errors.Count);

            var entries = await app.Uow.Inwards.GetByPeriodDetailedAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
            var entry = Assert.Single(entries);
            Assert.Single(entry.Items);
        }
    }

    [Fact]
    public async Task ImportInwardAsync_RejectsSerialAlreadyInDatabase()
    {
        var (app, customerCode) = await SeedMastersAsync();
        using (app)
        {
            var service = CreateService(app);

            using (var first = BuildWorkbook(new object[] { "01/01/2026", "DC-1", "INV-1", customerCode, "Monitor", 1, "DB-1", "Evaluation", "", "", "" }))
            {
                var ok = await service.ImportInwardAsync(first, "import.xlsx");
                Assert.True(ok.Success);
            }

            using (var second = BuildWorkbook(new object[] { "02/01/2026", "DC-2", "INV-2", customerCode, "Monitor", 1, "DB-1", "Evaluation", "", "", "" }))
            {
                var result = await service.ImportInwardAsync(second, "import2.xlsx");
                Assert.False(result.Success);
                Assert.Contains(result.Errors, e => e.Message.Contains("already exists in the system"));
            }
        }
    }

    [Fact]
    public async Task CreateImportTemplateAsync_ProducesOpenableWorkbookWithHeaders()
    {
        var app = new TestApp();
        using (app)
        {
            var service = CreateService(app);
            using var stream = await service.CreateImportTemplateAsync();

            Assert.True(stream.Length > 0, "Template stream must not be empty.");

            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();
            Assert.Equal("InwardItems", ws.Name);
            Assert.Equal("DATE", ws.Cell(1, 1).GetString());
            Assert.Equal("Items Received From", ws.Cell(1, 4).GetString());
            Assert.Equal("Purpose", ws.Cell(1, 8).GetString());
            Assert.Equal("Received By", ws.Cell(1, 10).GetString());
            Assert.Equal("Remarks", ws.Cell(1, 11).GetString());
        }
    }

    [Fact]
    public async Task ImportInwardAsync_ResolvesVendorPartyAsPurchase()
    {
        var app = new TestApp();
        using (app)
        {
            await app.Uow.Vendors.AddAsync(new Vendor
            {
                Code = "VEN-001",
                Name = "Supply Co",
                IsActive = true
            });
            await app.Uow.Items.AddAsync(new Item
            {
                Code = "ITM-B",
                Name = "Gloves",
                IsSerialTracked = false,
                IsActive = true
            });
            await app.Uow.SaveChangesAsync();

            var service = CreateService(app);
            using var stream = BuildWorkbook(
                new object[] { "01/01/2026", "DC-1", "PO-1", "VEN-001", "Gloves", 5, "", "Service", "", "", "" }
            );

            var result = await service.ImportInwardAsync(stream, "import.xlsx");

            Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
            var entries = await app.Uow.Inwards.GetByPeriodDetailedAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
            var entry = Assert.Single(entries);
            Assert.Equal(InwardDC.Domain.Enums.InwardType.Purchase, entry.InwardType);
            Assert.NotNull(entry.Vendor);
        }
    }

    [Fact]
    public async Task ImportInwardAsync_PartyNotFound_ReportsError()
    {
        var app = new TestApp();
        using (app)
        {
            var service = CreateService(app);
            using var stream = BuildWorkbook(
                new object[] { "01/01/2026", "DC-1", "INV-1", "Nobody Here", "Gloves", 5, "", "", "", "", "" }
            );

            var result = await service.ImportInwardAsync(stream, "import.xlsx");

            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Message.Contains("not found in customer or vendor master"));
        }
    }

    [Fact]
    public async Task ImportInwardAsync_UnknownPurpose_ReportsError()
    {
        var (app, customerCode) = await SeedMastersAsync();
        using (app)
        {
            var service = CreateService(app);
            using var stream = BuildWorkbook(
                new object[] { "01/01/2026", "DC-1", "INV-1", customerCode, "Gloves", 5, "", "No Such Purpose", "", "", "" }
            );

            var result = await service.ImportInwardAsync(stream, "import.xlsx");

            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Message.Contains("Purpose 'No Such Purpose' not found"));
        }
    }

    private static MemoryStream BuildDispatchWorkbook(params object[][] rows)
    {
        var stream = new MemoryStream();
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("DispatchItems");
        var headers = new[]
        {
            "DATE", "D.C No", "Invoice No", "Items Sent To", "Equipment",
            "Qty", "Serial No", "Purpose", "Payment Status", "Mode of Dispatch",
            "POD No", "Remarks"
        };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
                ws.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(rows[r][c]);

        wb.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task ImportDispatchesAsync_CreatesDcsGroupingRowsByHeader()
    {
        var (app, customerCode) = await SeedMastersAsync();
        using (app)
        {
            var service = CreateService(app);
            using var stream = BuildDispatchWorkbook(
                new object[] { "01/01/2026", "DC-1001", "INV-900", customerCode, "Monitor", 1, "M-0001", "Evaluation", "Pending", "By Hand", "POD-001", "dispatch remarks" },
                new object[] { "01/01/2026", "DC-1001", "INV-900", customerCode, "Gloves", 5, "", "Evaluation", "Pending", "By Hand", "POD-001", "" }
            );

            var result = await service.ImportDispatchesAsync(stream, "import.xlsx");

            Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
            Assert.Empty(result.Errors);
            Assert.Equal(2, result.ImportedRows);
            Assert.Equal(1, result.CreatedEntries);  // same header -> single DC

            var dcs = await app.Uow.DCs.GetByPeriodDetailedAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
            var dc = Assert.Single(dcs);
            Assert.Equal("DC-1001", dc.DcNo);
            Assert.Equal("INV-900", dc.InvoiceNo);
            Assert.Equal("Pending", dc.PaymentStatus);
            Assert.Equal("By Hand", dc.ModeOfDispatch);
            Assert.Equal("POD-001", dc.PodNo);
            Assert.NotNull(dc.Purpose);
            Assert.Equal("Evaluation", dc.Purpose!.Name);
            Assert.Equal(2, dc.Items.Count);
            Assert.Equal(6, dc.TotalQuantity);
            Assert.Equal("dispatch remarks", dc.Items.Single(i => i.ItemName == "Monitor").Remarks);
        }
    }

    [Fact]
    public async Task ImportDispatchesAsync_RejectsDuplicateSerialWithinFile()
    {
        var (app, customerCode) = await SeedMastersAsync();
        using (app)
        {
            var service = CreateService(app);
            using var stream = BuildDispatchWorkbook(
                new object[] { "01/01/2026", "DC-1", "INV-1", customerCode, "Monitor", 1, "DUP-1", "Evaluation", "", "", "", "" },
                new object[] { "02/01/2026", "DC-2", "INV-2", customerCode, "Monitor", 1, "DUP-1", "Evaluation", "", "", "", "" }
            );

            var result = await service.ImportDispatchesAsync(stream, "import.xlsx");

            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Message.Contains("Duplicate serial 'DUP-1'"));
        }
    }

    [Fact]
    public async Task ImportDispatchesAsync_PartyNotFound_ReportsError()
    {
        var app = new TestApp();
        using (app)
        {
            var service = CreateService(app);
            using var stream = BuildDispatchWorkbook(
                new object[] { "01/01/2026", "DC-1", "INV-1", "Nobody Here", "Gloves", 5, "", "", "", "", "", "" }
            );

            var result = await service.ImportDispatchesAsync(stream, "import.xlsx");

            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Message.Contains("not found in customer or vendor master"));
        }
    }

    [Fact]
    public async Task ImportDispatchesAsync_VendorParty_ReportsError()
    {
        var app = new TestApp();
        using (app)
        {
            await app.Uow.Vendors.AddAsync(new Vendor
            {
                Code = "VEN-001",
                Name = "Supply Co",
                IsActive = true
            });
            await app.Uow.SaveChangesAsync();

            var service = CreateService(app);
            using var stream = BuildDispatchWorkbook(
                new object[] { "01/01/2026", "DC-1", "INV-1", "VEN-001", "Gloves", 5, "", "", "", "", "", "" }
            );

            var result = await service.ImportDispatchesAsync(stream, "import.xlsx");

            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Message.Contains("dispatches must go to a customer"));
        }
    }

    [Fact]
    public async Task ImportDispatchesAsync_SerialAlreadyDispatched_ReportsError()
    {
        var (app, customerCode) = await SeedMastersAsync();
        using (app)
        {
            var service = CreateService(app);
            using (var first = BuildDispatchWorkbook(new object[] { "01/01/2026", "DC-1", "INV-1", customerCode, "Monitor", 1, "DB-1", "Evaluation", "", "", "", "" }))
            {
                var ok = await service.ImportDispatchesAsync(first, "import.xlsx");
                Assert.True(ok.Success);
            }

            using (var second = BuildDispatchWorkbook(new object[] { "02/01/2026", "DC-2", "INV-2", customerCode, "Monitor", 1, "DB-1", "Evaluation", "", "", "", "" }))
            {
                var result = await service.ImportDispatchesAsync(second, "import2.xlsx");
                Assert.False(result.Success);
                Assert.Contains(result.Errors, e => e.Message.Contains("already dispatched"));
            }
        }
    }

    [Fact]
    public async Task CreateDispatchImportTemplateAsync_ProducesWorkbookWithHeaders()
    {
        var app = new TestApp();
        using (app)
        {
            var service = CreateService(app);
            using var stream = await service.CreateDispatchImportTemplateAsync();

            Assert.True(stream.Length > 0, "Template stream must not be empty.");

            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();
            Assert.Equal("DispatchItems", ws.Name);
            Assert.Equal("DATE", ws.Cell(1, 1).GetString());
            Assert.Equal("Items Sent To", ws.Cell(1, 4).GetString());
            Assert.Equal("Equipment", ws.Cell(1, 5).GetString());
            Assert.Equal("Payment Status", ws.Cell(1, 9).GetString());
            Assert.Equal("Mode of Dispatch", ws.Cell(1, 10).GetString());
            Assert.Equal("POD No", ws.Cell(1, 11).GetString());
        }
    }
}
