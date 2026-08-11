using ClosedXML.Excel;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Exceptions;
using InwardDC.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace InwardDC.Infrastructure.Services;

/// <summary>
/// Excel bulk operations built on ClosedXML:
///   - Import inward entries with per-row validation and duplicate (serial) detection
///   - Export inward / dispatch lists, reports and audit logs
///   - Generate a ready-to-use import template
/// </summary>
public class ExcelService : IExcelService
{
    private const string SheetName = "InwardItems";

    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ExcelService> _logger;

    public ExcelService(IUnitOfWork uow, ICurrentUserService currentUser, ILogger<ExcelService> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public Task<Stream> CreateImportTemplateAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var stream = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.Worksheets.Add(SheetName);
            var headers = new[]
            {
                "Inward Date (dd/MM/yyyy)", "Inward Type (Customer Return / Purchase / Service In / Other)",
                "Customer Code or Name", "Vendor Code or Name", "Invoice No", "Invoice Date (dd/MM/yyyy)",
                "Challan No", "Item Code", "Item Name", "Make", "Model", "Serial Number",
                "Quantity", "Rate", "Amount", "HSN", "Remarks"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
                cell.Style.Font.FontColor = XLColor.White;
            }

            ws.Cell(2, 1).Value = DateTime.Today.ToString("dd/MM/yyyy");
            ws.Cell(2, 2).Value = "Customer Return";
            ws.Cell(2, 3).Value = "Sample Customer";
            ws.Cell(2, 8).Value = "ITM/2026/0001";
            ws.Cell(2, 9).Value = "Patient Monitor";
            ws.Cell(2, 12).Value = "SN-0001";
            ws.Cell(2, 13).Value = 1;
            ws.Cell(2, 14).Value = 1000;
            ws.Cell(2, 15).Value = 1000;

            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();

            wb.SaveAs(stream);
        }
        stream.Position = 0;
        return Task.FromResult<Stream>(stream);
    }

    public async Task<FileImportResult> ImportInwardAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        var result = new FileImportResult();
        var errors = new List<ImportRowError>();
        var validRows = new List<ImportRow>();
        var seenSerials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var wb = new XLWorkbook(stream);
        if (!wb.TryGetWorksheet(SheetName, out var ws))
            ws = wb.Worksheets.First();

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int r = 2; r <= lastRow; r++)
        {
            ct.ThrowIfCancellationRequested();
            result.TotalRows++;

            var empty = Enumerable.Range(1, 17).All(c => string.IsNullOrWhiteSpace(GetString(ws, r, c)));
            if (empty) continue;

            var row = new ImportRow { SheetRow = r };

            // Date
            var dateText = GetString(ws, r, 1);
            if (string.IsNullOrWhiteSpace(dateText) || !TryParseDate(dateText, out var date))
            {
                errors.Add(new ImportRowError { Row = r, Message = "Inward date is missing or invalid.", Value = dateText });
                continue;
            }
            row.InwardDate = date;

            // Type
            row.InwardType = ParseType(GetString(ws, r, 2));

            // Customer / vendor
            row.Customer = GetString(ws, r, 3);
            row.Vendor = GetString(ws, r, 4);
            if (row.InwardType == InwardType.Purchase && string.IsNullOrWhiteSpace(row.Vendor))
            {
                errors.Add(new ImportRowError { Row = r, Message = "Vendor is required for purchase type." });
                continue;
            }
            if (row.InwardType != InwardType.Purchase && string.IsNullOrWhiteSpace(row.Customer))
            {
                errors.Add(new ImportRowError { Row = r, Message = "Customer is required for this type." });
                continue;
            }

            row.InvoiceNo = GetString(ws, r, 5);
            var invDateText = GetString(ws, r, 6);
            row.InvoiceDate = string.IsNullOrWhiteSpace(invDateText) ? null : TryParseDate(invDateText, out var invDate) ? invDate : null;
            row.ChallanNo = GetString(ws, r, 7);
            row.ItemCode = GetString(ws, r, 8);
            row.ItemName = GetString(ws, r, 9);
            row.Make = GetString(ws, r, 10);
            row.Model = GetString(ws, r, 11);
            row.SerialNumber = GetString(ws, r, 12);

            // Quantity / rate / amount
            if (!TryGetDecimal(ws, r, 13, out var qty) || qty <= 0)
            {
                errors.Add(new ImportRowError { Row = r, Message = "Quantity must be a number greater than zero.", Value = GetString(ws, r, 13) });
                continue;
            }
            row.Quantity = qty;
            row.Rate = TryGetDecimal(ws, r, 14, out var rate) ? rate : 0;
            row.Amount = TryGetDecimal(ws, r, 15, out var amount) && amount > 0 ? amount : row.Quantity * row.Rate;
            row.Hsn = GetString(ws, r, 16);
            row.Remarks = GetString(ws, r, 17);

            if (string.IsNullOrWhiteSpace(row.ItemCode) && string.IsNullOrWhiteSpace(row.ItemName))
            {
                errors.Add(new ImportRowError { Row = r, Message = "Item code or item name is required." });
                continue;
            }

            // Duplicate serial within the file
            if (!string.IsNullOrWhiteSpace(row.SerialNumber))
            {
                var serialKey = row.SerialNumber.Trim();
                if (!seenSerials.Add(serialKey))
                {
                    result.DuplicatesSkipped++;
                    errors.Add(new ImportRowError { Row = r, Message = $"Duplicate serial '{serialKey}' within the file.", Value = serialKey });
                    continue;
                }
            }

            validRows.Add(row);
        }

        result.ImportedRows = validRows.Count;

        if (errors.Count > 0)
        {
            result.Errors = errors;
            return result;
        }

        // Resolve masters and detect duplicates against the database before creating anything.
        var databaseErrors = await ValidateAgainstDatabaseAsync(validRows, seenSerials, ct);
        if (databaseErrors.Count > 0)
        {
            result.Errors = databaseErrors;
            return result;
        }

        var created = await CreateInwardEntriesAsync(validRows, ct);
        result.CreatedEntries = created;
        result.Errors = Array.Empty<ImportRowError>();

        return result;
    }

    private async Task<List<ImportRowError>> ValidateAgainstDatabaseAsync(List<ImportRow> rows, HashSet<string> seenSerials, CancellationToken ct)
    {
        var errors = new List<ImportRowError>();

        // Serial existence check (all at once, grouped to avoid N+1 where possible)
        foreach (var serialGroup in rows.Where(r => !string.IsNullOrWhiteSpace(r.SerialNumber))
                     .GroupBy(r => r.SerialNumber!.Trim()))
        {
            if (await _uow.SerialNumbers.SerialExistsAsync(serialGroup.Key, ct))
            {
                var row = serialGroup.First();
                errors.Add(new ImportRowError
                {
                    Row = row.SheetRow,
                    Message = $"Serial number '{serialGroup.Key}' already exists in the system.",
                    Value = serialGroup.Key
                });
            }
        }

        if (errors.Count > 0) return errors;

        // Resolve customer / vendor / item references
        var customerCache = new Dictionary<string, Customer?>(StringComparer.OrdinalIgnoreCase);
        var vendorCache = new Dictionary<string, Vendor?>(StringComparer.OrdinalIgnoreCase);
        var itemCache = new Dictionary<string, Item?>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.Customer))
            {
                row.CustomerEntity = await ResolveCustomerAsync(row.Customer, customerCache, ct);
                if (row.CustomerEntity is null)
                {
                    errors.Add(new ImportRowError { Row = row.SheetRow, Message = $"Customer '{row.Customer}' not found in master.", Value = row.Customer });
                }
            }

            if (!string.IsNullOrWhiteSpace(row.Vendor))
            {
                row.VendorEntity = await ResolveVendorAsync(row.Vendor, vendorCache, ct);
                if (row.VendorEntity is null)
                {
                    errors.Add(new ImportRowError { Row = row.SheetRow, Message = $"Vendor '{row.Vendor}' not found in master.", Value = row.Vendor });
                }
            }

            var itemKey = row.ItemCode ?? row.ItemName;
            if (!string.IsNullOrWhiteSpace(itemKey))
            {
                row.ItemEntity = await ResolveItemAsync(row.ItemCode, row.ItemName, itemCache, ct);
                if (row.ItemEntity is null && !string.IsNullOrWhiteSpace(row.SerialNumber))
                {
                    errors.Add(new ImportRowError
                    {
                        Row = row.SheetRow,
                        Message = $"Item '{itemKey}' not found in master, and a serial number requires a master item.",
                        Value = itemKey
                    });
                }
            }

            if (errors.Count >= 50)
                break;
        }

        return errors;
    }

    private async Task<int> CreateInwardEntriesAsync(List<ImportRow> rows, CancellationToken ct)
    {
        var grouped = rows.GroupBy(r => new InwardGroupKey(r.InwardDate.Date, r.InwardType, r.CustomerEntity?.Id, r.VendorEntity?.Id, r.InvoiceNo, r.ChallanNo));
        var created = 0;

        var settings = await _uow.Settings.GetValueAsync("Numbering.InwardPrefix", ct) ?? "INW";
        var year = DateTime.Today.Year;

        foreach (var group in grouped)
        {
            var seq = await _uow.Sequences.GetNextAsync("Inward", settings, year, ct);
            var entry = new InwardEntry
            {
                InwardNo = $"{settings}/{year}/{seq:0000}",
                InwardDate = group.Key.Date,
                InwardType = group.Key.Type,
                CustomerId = group.Key.CustomerId,
                VendorId = group.Key.VendorId,
                ReferenceInvoiceNo = group.Key.InvoiceNo ?? string.Empty,
                ChallanNo = group.Key.ChallanNo ?? string.Empty,
                Status = InwardStatus.Received
            };

            await _uow.Inwards.AddAsync(entry, ct);

            foreach (var row in group)
            {
                var isSerialTracked = row.ItemEntity?.IsSerialTracked ?? !string.IsNullOrWhiteSpace(row.SerialNumber);
                var lineItem = new InwardItem
                {
                    ItemId = row.ItemEntity?.Id,
                    ItemName = row.ItemEntity?.Name ?? row.ItemName ?? string.Empty,
                    ItemMake = row.Make ?? string.Empty,
                    ItemModel = row.Model ?? string.Empty,
                    HsnCode = row.Hsn ?? string.Empty,
                    Unit = row.ItemEntity?.Unit ?? "Nos",
                    Quantity = row.Quantity,
                    Rate = row.Rate,
                    Amount = row.Amount,
                    Remarks = row.Remarks ?? string.Empty
                };

                if (!string.IsNullOrWhiteSpace(row.SerialNumber))
                {
                    lineItem.Serials.Add(new SerialNumber
                    {
                        ItemId = row.ItemEntity!.Id,
                        SerialNo = row.SerialNumber.Trim(),
                        Status = SerialStatus.InStock,
                        InwardEntryId = entry.Id,
                        Notes = row.Remarks ?? string.Empty
                    });
                }

                entry.Items.Add(lineItem);

                await _uow.ItemEvents.AddAsync(new ItemEvent
                {
                    ItemId = row.ItemEntity?.Id ?? Guid.Empty,
                    SerialNo = row.SerialNumber ?? string.Empty,
                    EventType = ItemEventType.InwardReceived,
                    ReferenceType = nameof(InwardEntry),
                    ReferenceId = entry.Id,
                    ReferenceNumber = entry.InwardNo,
                    Quantity = row.Quantity,
                    Notes = lineItem.ItemName,
                    EventedBy = _currentUser.UserId,
                    EventedOn = DateTime.UtcNow
                }, ct);
            }

            entry.TotalQuantity = entry.Items.Sum(i => i.Quantity);
            entry.TotalAmount = entry.Items.Sum(i => i.Amount);
            created++;
        }

        await _uow.SaveChangesAsync(ct);
        return created;
    }

    private async Task<Customer?> ResolveCustomerAsync(string value, Dictionary<string, Customer?> cache, CancellationToken ct)
    {
        if (cache.TryGetValue(value, out var cached)) return cached;
        var customer = await _uow.Customers.GetByCodeAsync(value, ct)
            ?? (await _uow.Customers.GetPagedAsync(new CustomerSearchFilter { SearchText = value, PageSize = 5 }, ct)).Items.FirstOrDefault();
        cache[value] = customer;
        return customer;
    }

    private async Task<Vendor?> ResolveVendorAsync(string value, Dictionary<string, Vendor?> cache, CancellationToken ct)
    {
        if (cache.TryGetValue(value, out var cached)) return cached;
        var vendor = await _uow.Vendors.GetByCodeAsync(value, ct)
            ?? (await _uow.Vendors.GetPagedAsync(new VendorSearchFilter { SearchText = value, PageSize = 5 }, ct)).Items.FirstOrDefault();
        cache[value] = vendor;
        return vendor;
    }

    private async Task<Item?> ResolveItemAsync(string? code, string? name, Dictionary<string, Item?> cache, CancellationToken ct)
    {
        var key = (code ?? name)!;
        if (cache.TryGetValue(key, out var cached)) return cached;

        Item? item = null;
        if (!string.IsNullOrWhiteSpace(code))
            item = await _uow.Items.GetByCodeAsync(code.Trim(), ct);

        item ??= !string.IsNullOrWhiteSpace(name)
            ? (await _uow.Items.GetPagedAsync(new ItemSearchFilter { SearchText = name, PageSize = 5 }, ct)).Items.FirstOrDefault()
            : null;

        cache[key] = item;
        return item;
    }

    public async Task<string> ExportInwardsAsync(InwardSearchFilter filter, string filePath, CancellationToken ct = default)
    {
        filter.Page = 1;
        filter.PageSize = 10000;
        var data = await _uow.Inwards.GetPagedAsync(filter, ct);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Inwards");
        WriteRow(ws, 1, new[] { "Inward No", "Date", "Type", "Party", "Invoice No", "Invoice Date", "Challan No", "Total Qty", "Amount", "Status", "Remarks", "Created On" });

        int r = 2;
        foreach (var x in data.Items)
        {
            WriteRow(ws, r++, new object[]
            {
                x.InwardNo, x.InwardDate.ToShortDateString(), x.InwardType.ToString(),
                x.Customer?.Name ?? x.Vendor?.Name ?? "", x.ReferenceInvoiceNo,
                x.ReferenceInvoiceDate?.ToShortDateString() ?? "", x.ChallanNo,
                x.TotalQuantity, x.TotalAmount, x.Status.ToString(), x.Remarks, x.CreatedOn.ToShortDateString()
            });
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
        return filePath;
    }

    public async Task<string> ExportDispatchesAsync(DispatchSearchFilter filter, string filePath, CancellationToken ct = default)
    {
        filter.Page = 1;
        filter.PageSize = 10000;
        var data = await _uow.DCs.GetPagedAsync(filter, ct);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Dispatch Challans");
        WriteRow(ws, 1, new[] { "DC No", "Date", "Customer", "Source Inward", "Reference Challan", "Total Qty", "Amount", "Status", "Remarks", "Created On" });

        int r = 2;
        foreach (var x in data.Items)
        {
            WriteRow(ws, r++, new object[]
            {
                x.DcNo, x.DcDate.ToShortDateString(), x.Customer?.Name ?? "", x.SourceInwardEntry?.InwardNo ?? "",
                x.ReferenceChallanNo, x.TotalQuantity, x.TotalAmount, x.Status.ToString(), x.Remarks, x.CreatedOn.ToShortDateString()
            });
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
        return filePath;
    }

    public Task<string> ExportReportAsync(string title, IReadOnlyList<ReportRowDto> rows, string filePath, CancellationToken ct = default)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Report");
        ws.Cell(1, 1).Value = title;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        WriteRow(ws, 3, new[] { "Date", "Number", "Type", "Party", "Item", "Serial No", "Qty", "Unit", "Rate", "Amount", "Status" });

        int r = 4;
        decimal total = 0;
        foreach (var row in rows)
        {
            WriteRow(ws, r++, new object[]
            {
                row.Date.ToShortDateString(), row.Number, row.Type, row.Party, row.ItemName, row.SerialNo,
                row.Quantity, row.Unit, row.Rate, row.Amount, row.Status
            });
            total += row.Amount;
        }

        WriteRow(ws, r, new object[] { "", "", "", "", "TOTAL", "", "", "", "", total, "" });
        ws.Cell(r, 10).Style.Font.Bold = true;

        ws.SheetView.FreezeRows(3);
        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
        return Task.FromResult(filePath);
    }

    public Task<string> ExportAuditLogsAsync(IReadOnlyList<AuditLogDto> rows, string filePath, CancellationToken ct = default)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Audit Logs");
        WriteRow(ws, 1, new[] { "Timestamp", "User", "Action", "Entity", "Description", "Machine" });

        int r = 2;
        foreach (var x in rows)
        {
            WriteRow(ws, r++, new object[]
            {
                x.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"), x.FullName, x.ActionName,
                x.EntityType, x.Description, x.IpAddress
            });
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
        return Task.FromResult(filePath);
    }

    private static void WriteRow(IXLWorksheet ws, int row, object[] values)
    {
        for (int i = 0; i < values.Length; i++)
            ws.Cell(row, i + 1).Value = XLCellValue.FromObject(values[i]);
    }

    private static string GetString(IXLWorksheet ws, int row, int col)
    {
        var cell = ws.Cell(row, col);
        return cell.IsEmpty() ? string.Empty : cell.GetFormattedString();
    }

    private static bool TryGetDecimal(IXLWorksheet ws, int row, int col, out decimal value)
    {
        var cell = ws.Cell(row, col);
        if (cell.IsEmpty())
        {
            value = 0;
            return false;
        }
        return decimal.TryParse(cell.GetFormattedString(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseDate(string text, out DateTime date)
    {
        return DateTime.TryParseExact(text.Trim(), new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy-MM-dd" },
            System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date);
    }

    private static InwardType ParseType(string text)
    {
        var t = text.Trim().ToLowerInvariant();
        if (t.Contains("purchase")) return InwardType.Purchase;
        if (t.Contains("service")) return InwardType.ServiceIn;
        if (t.Contains("return")) return InwardType.CustomerReturn;
        return InwardType.Other;
    }

    private sealed class ImportRow
    {
        public int SheetRow { get; set; }
        public DateTime InwardDate { get; set; }
        public InwardType InwardType { get; set; }
        public string? Customer { get; set; }
        public string? Vendor { get; set; }
        public string? InvoiceNo { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? ChallanNo { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public string? Hsn { get; set; }
        public string? Remarks { get; set; }
        public Customer? CustomerEntity { get; set; }
        public Vendor? VendorEntity { get; set; }
        public Item? ItemEntity { get; set; }
    }

    private sealed record InwardGroupKey(DateTime Date, InwardType Type, Guid? CustomerId, Guid? VendorId, string? InvoiceNo, string? ChallanNo);
}
