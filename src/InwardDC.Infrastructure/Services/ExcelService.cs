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
    private const string DispatchSheetName = "DispatchItems";

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
                "DATE", "D.C No", "Invoice No", "Items Received From", "Name of Item",
                "Qty", "Serial No", "Purpose", "Remarks", "Received By", "Remarks"
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
            ws.Cell(2, 4).Value = "Sample Customer";
            ws.Cell(2, 5).Value = "Patient Monitor";
            ws.Cell(2, 6).Value = 1;
            ws.Cell(2, 7).Value = "SN-0001";
            ws.Cell(2, 8).Value = "Evaluation";
            ws.Cell(2, 10).Value = "Your Name";

            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();

            wb.SaveAs(stream);
        }
        stream.Position = 0;
        return Task.FromResult<Stream>(stream);
    }

    public Task<Stream> CreateDispatchImportTemplateAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var stream = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            var ws = wb.Worksheets.Add(DispatchSheetName);
            var headers = new[]
            {
                "DATE", "D.C No", "Invoice No", "Items Sent To", "Equipment",
                "Qty", "Serial No", "Purpose", "Payment Status", "Mode of Dispatch",
                "POD No", "Remarks"
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
            ws.Cell(2, 2).Value = "DC-1001";
            ws.Cell(2, 3).Value = "INV-900";
            ws.Cell(2, 4).Value = "Sample Customer";
            ws.Cell(2, 5).Value = "Patient Monitor";
            ws.Cell(2, 6).Value = 1;
            ws.Cell(2, 7).Value = "SN-0001";
            ws.Cell(2, 8).Value = "Evaluation";
            ws.Cell(2, 9).Value = "Pending";
            ws.Cell(2, 10).Value = "By Hand";
            ws.Cell(2, 11).Value = "POD-001";

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

            var empty = Enumerable.Range(1, 11).All(c => string.IsNullOrWhiteSpace(GetString(ws, r, c)));
            if (empty) continue;

            var row = new ImportRow { SheetRow = r };

            // 1. Date
            var dateText = GetString(ws, r, 1);
            if (string.IsNullOrWhiteSpace(dateText) || !TryParseDate(dateText, out var date))
            {
                errors.Add(new ImportRowError { Row = r, Message = "Date is missing or invalid.", Value = dateText });
                continue;
            }
            row.InwardDate = date;

            // 2. D.C No -> challan number, 3. Invoice No
            row.ChallanNo = GetString(ws, r, 2);
            row.InvoiceNo = GetString(ws, r, 3);

            // 4. Items Received From (resolved against customer OR vendor master)
            row.Party = GetString(ws, r, 4);
            if (string.IsNullOrWhiteSpace(row.Party))
            {
                errors.Add(new ImportRowError { Row = r, Message = "Items Received From is required.", Value = row.Party });
                continue;
            }

            // 5. Name of item
            row.ItemName = GetString(ws, r, 5);
            if (string.IsNullOrWhiteSpace(row.ItemName))
            {
                errors.Add(new ImportRowError { Row = r, Message = "Name of Item is required.", Value = row.ItemName });
                continue;
            }

            // 6. Quantity
            if (!TryGetDecimal(ws, r, 6, out var qty) || qty <= 0)
            {
                errors.Add(new ImportRowError { Row = r, Message = "Qty must be a number greater than zero.", Value = GetString(ws, r, 6) });
                continue;
            }
            row.Quantity = qty;

            // 7. Serial No
            row.SerialNumber = GetString(ws, r, 7);

            // 8. Purpose (resolved by name; optional)
            row.PurposeName = GetString(ws, r, 8);

            // 9. Remarks (line)  |  10. Received By (header)  |  11. Remarks (header)
            row.LineRemarks = GetString(ws, r, 9);
            row.ReceivedBy = GetString(ws, r, 10);
            row.HeaderRemarks = GetString(ws, r, 11);

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

    public async Task<FileImportResult> ImportDispatchesAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        var result = new FileImportResult();
        var errors = new List<ImportRowError>();
        var validRows = new List<DispatchImportRow>();
        var seenSerials = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var wb = new XLWorkbook(stream);
        if (!wb.TryGetWorksheet(DispatchSheetName, out var ws))
            ws = wb.Worksheets.First();

        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int r = 2; r <= lastRow; r++)
        {
            ct.ThrowIfCancellationRequested();
            result.TotalRows++;

            var empty = Enumerable.Range(1, 12).All(c => string.IsNullOrWhiteSpace(GetString(ws, r, c)));
            if (empty) continue;

            var row = new DispatchImportRow { SheetRow = r };

            // 1. Date
            var dateText = GetString(ws, r, 1);
            if (string.IsNullOrWhiteSpace(dateText) || !TryParseDate(dateText, out var date))
            {
                errors.Add(new ImportRowError { Row = r, Message = "Date is missing or invalid.", Value = dateText });
                continue;
            }
            row.DcDate = date;

            // 2. D.C No (the challan number, kept as provided)
            row.DcNo = GetString(ws, r, 2);
            if (string.IsNullOrWhiteSpace(row.DcNo))
            {
                errors.Add(new ImportRowError { Row = r, Message = "D.C No is required.", Value = row.DcNo });
                continue;
            }

            // 3. Invoice No
            row.InvoiceNo = GetString(ws, r, 3);

            // 4. Items Sent To (customer master first, then vendor master)
            row.Party = GetString(ws, r, 4);
            if (string.IsNullOrWhiteSpace(row.Party))
            {
                errors.Add(new ImportRowError { Row = r, Message = "Items Sent To is required.", Value = row.Party });
                continue;
            }

            // 5. Equipment
            row.ItemName = GetString(ws, r, 5);
            if (string.IsNullOrWhiteSpace(row.ItemName))
            {
                errors.Add(new ImportRowError { Row = r, Message = "Equipment is required.", Value = row.ItemName });
                continue;
            }

            // 6. Quantity
            if (!TryGetDecimal(ws, r, 6, out var qty) || qty <= 0)
            {
                errors.Add(new ImportRowError { Row = r, Message = "Qty must be a number greater than zero.", Value = GetString(ws, r, 6) });
                continue;
            }
            row.Quantity = qty;

            // 7. Serial No
            row.SerialNumber = GetString(ws, r, 7);

            // 8. Purpose | 9. Payment Status | 10. Mode of Dispatch | 11. POD No | 12. Remarks
            row.PurposeName = GetString(ws, r, 8);
            row.PaymentStatus = GetString(ws, r, 9);
            row.ModeOfDispatch = GetString(ws, r, 10);
            row.PodNo = GetString(ws, r, 11);
            row.Remarks = GetString(ws, r, 12);

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

        var databaseErrors = await ValidateDispatchRowsAsync(validRows, ct);
        if (databaseErrors.Count > 0)
        {
            result.Errors = databaseErrors;
            return result;
        }

        var created = await CreateDispatchEntriesAsync(validRows, ct);
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

        // Resolve party (customer or vendor), item and purpose references
        var customerCache = new Dictionary<string, Customer?>(StringComparer.OrdinalIgnoreCase);
        var vendorCache = new Dictionary<string, Vendor?>(StringComparer.OrdinalIgnoreCase);
        var itemCache = new Dictionary<string, Item?>(StringComparer.OrdinalIgnoreCase);
        var purposeCache = new Dictionary<string, Purpose?>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.Party))
            {
                // Items Received From is matched against customers first, then vendors.
                row.CustomerEntity = await ResolveCustomerAsync(row.Party, customerCache, ct);
                if (row.CustomerEntity is null)
                    row.VendorEntity = await ResolveVendorAsync(row.Party, vendorCache, ct);

                if (row.CustomerEntity is null && row.VendorEntity is null)
                {
                    errors.Add(new ImportRowError { Row = row.SheetRow, Message = $"Items Received From '{row.Party}' not found in customer or vendor master.", Value = row.Party });
                }
                else
                {
                    row.InwardType = row.VendorEntity is not null ? InwardType.Purchase : InwardType.CustomerReturn;
                }
            }

            var itemKey = row.ItemName;
            if (!string.IsNullOrWhiteSpace(itemKey))
            {
                row.ItemEntity = await ResolveItemAsync(null, row.ItemName, itemCache, ct);
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

            if (!string.IsNullOrWhiteSpace(row.PurposeName))
            {
                row.PurposeEntity = await ResolvePurposeAsync(row.PurposeName, purposeCache, ct);
                if (row.PurposeEntity is null)
                {
                    errors.Add(new ImportRowError { Row = row.SheetRow, Message = $"Purpose '{row.PurposeName}' not found in master.", Value = row.PurposeName });
                }
            }

            if (errors.Count >= 50)
                break;
        }

        return errors;
    }

    private async Task<int> CreateInwardEntriesAsync(List<ImportRow> rows, CancellationToken ct)
    {
        var grouped = rows.GroupBy(r => new InwardGroupKey(r.InwardDate.Date, r.InwardType, r.CustomerEntity?.Id, r.VendorEntity?.Id, r.PurposeEntity?.Id, r.InvoiceNo, r.ChallanNo));
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
                PurposeId = group.Key.PurposeId,
                ReferenceInvoiceNo = group.Key.InvoiceNo ?? string.Empty,
                ChallanNo = group.Key.ChallanNo ?? string.Empty,
                ReceivedBy = group.First().ReceivedBy ?? string.Empty,
                Remarks = group.First().HeaderRemarks ?? string.Empty,
                Status = InwardStatus.Received
            };

            await _uow.Inwards.AddAsync(entry, ct);

            foreach (var row in group)
            {
                var lineItem = new InwardItem
                {
                    ItemId = row.ItemEntity?.Id,
                    ItemName = row.ItemEntity?.Name ?? row.ItemName ?? string.Empty,
                    ItemMake = row.Make ?? string.Empty,
                    ItemModel = row.Model ?? string.Empty,
                    HsnCode = string.Empty,
                    Unit = row.ItemEntity?.Unit ?? "Nos",
                    Quantity = row.Quantity,
                    Rate = 0,
                    Amount = 0,
                    Remarks = row.LineRemarks ?? string.Empty
                };

                if (!string.IsNullOrWhiteSpace(row.SerialNumber))
                {
                    lineItem.Serials.Add(new SerialNumber
                    {
                        ItemId = row.ItemEntity!.Id,
                        SerialNo = row.SerialNumber.Trim(),
                        Status = SerialStatus.InStock,
                        InwardEntryId = entry.Id,
                        Notes = row.LineRemarks ?? string.Empty
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

    private async Task<List<ImportRowError>> ValidateDispatchRowsAsync(List<DispatchImportRow> rows, CancellationToken ct)
    {
        var errors = new List<ImportRowError>();

        var customerCache = new Dictionary<string, Customer?>(StringComparer.OrdinalIgnoreCase);
        var vendorCache = new Dictionary<string, Vendor?>(StringComparer.OrdinalIgnoreCase);
        var itemCache = new Dictionary<string, Item?>(StringComparer.OrdinalIgnoreCase);
        var purposeCache = new Dictionary<string, Purpose?>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.Party))
            {
                // Items Sent To is matched against customers first, then vendors.
                row.CustomerEntity = await ResolveCustomerAsync(row.Party, customerCache, ct);
                if (row.CustomerEntity is null)
                    row.VendorEntity = await ResolveVendorAsync(row.Party, vendorCache, ct);

                if (row.CustomerEntity is null && row.VendorEntity is null)
                {
                    errors.Add(new ImportRowError { Row = row.SheetRow, Message = $"Items Sent To '{row.Party}' not found in customer or vendor master.", Value = row.Party });
                }
                else if (row.CustomerEntity is null)
                {
                    errors.Add(new ImportRowError { Row = row.SheetRow, Message = $"Items Sent To '{row.Party}' is a vendor; dispatches must go to a customer.", Value = row.Party });
                }
            }

            if (!string.IsNullOrWhiteSpace(row.ItemName))
            {
                row.ItemEntity = await ResolveItemAsync(null, row.ItemName, itemCache, ct);
                if (row.ItemEntity is null && !string.IsNullOrWhiteSpace(row.SerialNumber))
                {
                    errors.Add(new ImportRowError
                    {
                        Row = row.SheetRow,
                        Message = $"Item '{row.ItemName}' not found in master, and a serial number requires a master item.",
                        Value = row.ItemName
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(row.PurposeName))
            {
                row.PurposeEntity = await ResolvePurposeAsync(row.PurposeName, purposeCache, ct);
                if (row.PurposeEntity is null)
                {
                    errors.Add(new ImportRowError { Row = row.SheetRow, Message = $"Purpose '{row.PurposeName}' not found in master.", Value = row.PurposeName });
                }
            }

            if (!string.IsNullOrWhiteSpace(row.SerialNumber))
            {
                var existing = await _uow.SerialNumbers.GetBySerialAsync(row.SerialNumber.Trim(), ct);
                if (existing is not null && existing.Status == SerialStatus.Dispatched)
                {
                    errors.Add(new ImportRowError { Row = row.SheetRow, Message = $"Serial '{row.SerialNumber.Trim()}' is already dispatched.", Value = row.SerialNumber });
                }
                row.ExistingSerial = existing;
            }

            if (errors.Count >= 50)
                break;
        }

        return errors;
    }

    private async Task<int> CreateDispatchEntriesAsync(List<DispatchImportRow> rows, CancellationToken ct)
    {
        var grouped = rows.GroupBy(r => new DispatchGroupKey(
            r.DcDate.Date, r.DcNo!.Trim(), r.CustomerEntity!.Id, r.InvoiceNo, r.PurposeEntity?.Id,
            r.PaymentStatus, r.ModeOfDispatch, r.PodNo));

        var affectedInwardIds = new HashSet<Guid>();
        var created = 0;

        foreach (var group in grouped)
        {
            var dc = new DispatchChallan
            {
                DcNo = group.Key.DcNo,
                DcDate = group.Key.Date,
                CustomerId = group.Key.CustomerId,
                InvoiceNo = group.Key.InvoiceNo ?? string.Empty,
                PurposeId = group.Key.PurposeId,
                PaymentStatus = group.Key.PaymentStatus ?? string.Empty,
                ModeOfDispatch = group.Key.ModeOfDispatch ?? string.Empty,
                PodNo = group.Key.PodNo ?? string.Empty,
                Status = DispatchStatus.Generated
            };

            await _uow.DCs.AddAsync(dc, ct);

            foreach (var row in group)
            {
                var lineItem = new DispatchItem
                {
                    DispatchChallanId = dc.Id,
                    ItemId = row.ItemEntity?.Id,
                    ItemName = row.ItemEntity?.Name ?? row.ItemName ?? string.Empty,
                    ItemMake = row.ItemEntity?.Make ?? string.Empty,
                    ItemModel = row.ItemEntity?.Model ?? string.Empty,
                    HsnCode = row.ItemEntity?.HsnCode ?? string.Empty,
                    Unit = row.ItemEntity?.Unit ?? "Nos",
                    Quantity = row.Quantity,
                    Rate = 0,
                    Amount = 0,
                    Remarks = row.Remarks ?? string.Empty
                };

                if (!string.IsNullOrWhiteSpace(row.SerialNumber))
                {
                    var serialNo = row.SerialNumber.Trim();
                    SerialNumber serial;
                    if (row.ExistingSerial is not null)
                    {
                        serial = row.ExistingSerial;
                        serial.Status = SerialStatus.Dispatched;
                        serial.DispatchChallanId = dc.Id;
                        serial.DispatchItemId = lineItem.Id;
                        serial.DispatchedOn = DateTime.UtcNow;
                        if (serial.InwardItemId.HasValue)
                        {
                            var inwardItem = await _uow.Inwards.GetInwardItemForUpdateAsync(serial.InwardItemId.Value, ct);
                            if (inwardItem is not null)
                            {
                                inwardItem.DispatchedQuantity += 1;
                                affectedInwardIds.Add(inwardItem.InwardEntryId);
                            }
                        }
                    }
                    else
                    {
                        serial = new SerialNumber
                        {
                            ItemId = row.ItemEntity!.Id,
                            SerialNo = serialNo,
                            Status = SerialStatus.Dispatched,
                            DispatchChallanId = dc.Id,
                            DispatchItemId = lineItem.Id,
                            DispatchedOn = DateTime.UtcNow,
                            Notes = row.Remarks ?? string.Empty
                        };
                    }
                    lineItem.Serials.Add(serial);
                }

                dc.Items.Add(lineItem);

                await _uow.ItemEvents.AddAsync(new ItemEvent
                {
                    ItemId = row.ItemEntity?.Id ?? Guid.Empty,
                    SerialNo = row.SerialNumber ?? string.Empty,
                    EventType = ItemEventType.Dispatched,
                    ReferenceType = nameof(DispatchChallan),
                    ReferenceId = dc.Id,
                    ReferenceNumber = dc.DcNo,
                    Quantity = row.Quantity,
                    Notes = lineItem.ItemName,
                    EventedBy = _currentUser.UserId,
                    EventedOn = DateTime.UtcNow
                }, ct);
            }

            dc.TotalQuantity = dc.Items.Sum(i => i.Quantity);
            dc.TotalAmount = dc.Items.Sum(i => i.Amount);
            created++;
        }

        foreach (var inwardId in affectedInwardIds)
            await RecomputeInwardStatusAsync(inwardId, ct);

        await _uow.SaveChangesAsync(ct);
        return created;
    }

    private async Task RecomputeInwardStatusAsync(Guid inwardId, CancellationToken ct)
    {
        var entry = await _uow.Inwards.GetForUpdateAsync(inwardId, ct);
        if (entry is null || entry.IsDeleted || entry.Status == InwardStatus.Cancelled)
            return;

        var activeItems = entry.Items.Where(i => !i.IsDeleted).ToList();
        if (activeItems.Count == 0) return;

        var allDispatched = activeItems.All(i => i.DispatchedQuantity >= i.Quantity && i.Quantity > 0);
        var anyDispatched = activeItems.Any(i => i.DispatchedQuantity > 0);

        entry.Status = allDispatched
            ? InwardStatus.FullyDispatched
            : anyDispatched
                ? InwardStatus.PartiallyDispatched
                : InwardStatus.Received;
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

    private async Task<Purpose?> ResolvePurposeAsync(string value, Dictionary<string, Purpose?> cache, CancellationToken ct)
    {
        var key = value.Trim();
        if (cache.TryGetValue(key, out var cached)) return cached;
        var purpose = await _uow.Purposes.GetByNameAsync(key, ct);
        cache[key] = purpose;
        return purpose;
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
        WriteRow(ws, 1, new[] { "DC No", "Date", "Customer", "Source Inward", "Reference Challan", "Invoice No", "Payment Status", "Mode of Dispatch", "POD No", "Total Qty", "Amount", "Status", "Remarks", "Created On" });

        int r = 2;
        foreach (var x in data.Items)
        {
            WriteRow(ws, r++, new object[]
            {
                x.DcNo, x.DcDate.ToShortDateString(), x.Customer?.Name ?? "", x.SourceInwardEntry?.InwardNo ?? "",
                x.ReferenceChallanNo, x.InvoiceNo, x.PaymentStatus, x.ModeOfDispatch, x.PodNo,
                x.TotalQuantity, x.TotalAmount, x.Status.ToString(), x.Remarks, x.CreatedOn.ToShortDateString()
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

    private sealed class ImportRow
    {
        public int SheetRow { get; set; }
        public DateTime InwardDate { get; set; }
        public InwardType InwardType { get; set; }
        public string? Party { get; set; }
        public string? InvoiceNo { get; set; }
        public string? ChallanNo { get; set; }
        public string? ItemName { get; set; }
        public string? SerialNumber { get; set; }
        public decimal Quantity { get; set; }
        public string? PurposeName { get; set; }
        public string? LineRemarks { get; set; }
        public string? ReceivedBy { get; set; }
        public string? HeaderRemarks { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public Customer? CustomerEntity { get; set; }
        public Vendor? VendorEntity { get; set; }
        public Purpose? PurposeEntity { get; set; }
        public Item? ItemEntity { get; set; }
    }

    private sealed record InwardGroupKey(DateTime Date, InwardType Type, Guid? CustomerId, Guid? VendorId, Guid? PurposeId, string? InvoiceNo, string? ChallanNo);

    private sealed class DispatchImportRow
    {
        public int SheetRow { get; set; }
        public DateTime DcDate { get; set; }
        public string? DcNo { get; set; }
        public string? InvoiceNo { get; set; }
        public string? Party { get; set; }
        public string? ItemName { get; set; }
        public decimal Quantity { get; set; }
        public string? SerialNumber { get; set; }
        public string? PurposeName { get; set; }
        public string? PaymentStatus { get; set; }
        public string? ModeOfDispatch { get; set; }
        public string? PodNo { get; set; }
        public string? Remarks { get; set; }
        public Customer? CustomerEntity { get; set; }
        public Vendor? VendorEntity { get; set; }
        public Purpose? PurposeEntity { get; set; }
        public Item? ItemEntity { get; set; }
        public SerialNumber? ExistingSerial { get; set; }
    }

    private sealed record DispatchGroupKey(DateTime Date, string DcNo, Guid CustomerId, string? InvoiceNo, Guid? PurposeId, string? PaymentStatus, string? ModeOfDispatch, string? PodNo);
}
