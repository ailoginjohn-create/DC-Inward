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
            "Inward Date", "Inward Type", "Customer", "Vendor", "Invoice No", "Invoice Date",
            "Challan No", "Item Code", "Item Name", "Make", "Model", "Serial Number",
            "Quantity", "Rate", "Amount", "HSN", "Remarks"
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

    private async Task<(TestApp app, string customerCode)> SeedMastersAsync()
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
                new object[] { "01/01/2026", "Customer Return", customerCode, "", "INV-900", "", "CH-1", "ITM-A", "Monitor", "", "", "M-0001", 1, 1000, 1000, "", "" },
                new object[] { "01/01/2026", "Customer Return", customerCode, "", "INV-900", "", "CH-1", "ITM-B", "Gloves", "", "", "", 5, 20, 100, "", "" }
            );

            var result = await service.ImportInwardAsync(stream, "import.xlsx");

            Assert.True(result.Success);
            Assert.Empty(result.Errors);
            Assert.Equal(2, result.ImportedRows);
            Assert.Equal(1, result.CreatedEntries);  // same header -> single inward entry

            var entries = await app.Uow.Inwards.GetByPeriodDetailedAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
            var entry = Assert.Single(entries);
            Assert.StartsWith("INW/", entry.InwardNo);
            Assert.Equal(2, entry.Items.Count);
            Assert.Equal(6, entry.TotalQuantity);
            Assert.Equal(1100, entry.TotalAmount);
        }
    }

    [Fact]
    public async Task ImportInwardAsync_RejectsDuplicateSerialWithinFile()
    {
        var (app, customerCode) = await SeedMastersAsync();
        using (app)
        {
            var service = CreateService(app);
            using var stream = BuildWorkbook(
                new object[] { "01/01/2026", "Customer Return", customerCode, "", "INV-1", "", "CH-1", "ITM-A", "Monitor", "", "", "DUP-1", 1, 100, 100, "", "" },
                new object[] { "01/01/2026", "Customer Return", customerCode, "", "INV-2", "", "CH-2", "ITM-A", "Monitor", "", "", "DUP-1", 1, 100, 100, "", "" }
            );

            var result = await service.ImportInwardAsync(stream, "import.xlsx");

            Assert.False(result.Success);
            Assert.Equal(0, result.CreatedEntries);
            Assert.Contains(result.Errors, e => e.Message.Contains("Duplicate serial 'DUP-1'"));
        }
    }

    [Fact]
    public async Task ImportInwardAsync_RejectsSerialAlreadyInDatabase()
    {
        var (app, customerCode) = await SeedMastersAsync();
        using (app)
        {
            var service = CreateService(app);

            using (var first = BuildWorkbook(new object[] { "01/01/2026", "Customer Return", customerCode, "", "INV-1", "", "CH-1", "ITM-A", "Monitor", "", "", "DB-1", 1, 100, 100, "", "" }))
            {
                var ok = await service.ImportInwardAsync(first, "import.xlsx");
                Assert.True(ok.Success);
            }

            using (var second = BuildWorkbook(new object[] { "02/01/2026", "Customer Return", customerCode, "", "INV-2", "", "CH-2", "ITM-A", "Monitor", "", "", "DB-1", 1, 100, 100, "", "" }))
            {
                var result = await service.ImportInwardAsync(second, "import2.xlsx");
                Assert.False(result.Success);
                Assert.Contains(result.Errors, e => e.Message.Contains("already exists in the system"));
            }
        }
    }

    [Fact]
    public async Task ImportInwardAsync_RequiresVendorForPurchase()
    {
        var (app, _) = await SeedMastersAsync();
        using (app)
        {
            var service = CreateService(app);
            using var stream = BuildWorkbook(
                new object[] { "01/01/2026", "Purchase", "", "", "PO-1", "", "CH-1", "ITM-B", "Gloves", "", "", "", 5, 20, 100, "", "" }
            );

            var result = await service.ImportInwardAsync(stream, "import.xlsx");

            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Message.Contains("Vendor is required"));
        }
    }
}
