using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Exceptions;
using InwardDC.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace InwardDC.Application.Services;

public class InwardService : IInwardService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly ISettingsService _settings;
    private readonly ILogger<InwardService> _logger;

    public InwardService(IUnitOfWork uow, ICurrentUserService currentUser, IAuditService audit,
        ISettingsService settings, ILogger<InwardService> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _audit = audit;
        _settings = settings;
        _logger = logger;
    }

    public async Task<PagedResponse<InwardDto>> GetPagedAsync(InwardSearchFilter filter, CancellationToken ct = default)
    {
        var result = await _uow.Inwards.GetPagedAsync(filter, ct);
        var page = new PagedResult<InwardDto>
        {
            Items = result.Items.Select(x => ToDto(x, includeItems: false)).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
        return PagedResponse<InwardDto>.From(page);
    }

    public async Task<InwardDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await _uow.Inwards.GetByIdAsync(id, ct);
        return entry is null ? null : ToDto(entry, includeItems: true);
    }

    public async Task<string> PreviewNextNumberAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetCompanySettingsAsync(ct);
        var year = DateTime.Today.Year;
        var counter = await _uow.Sequences.GetCurrentAsync("Inward", s.InwardNumberPrefix, year, ct);
        var next = counter?.LastNumber + 1 ?? 1;
        return $"{s.InwardNumberPrefix}/{year}/{next:0000}";
    }

    public async Task<OperationResult> SaveAsync(InwardSaveRequest request, CancellationToken ct = default)
    {
        Validate(request);

        if (request.Id.HasValue)
            return await UpdateAsync(request, ct);

        var entry = new InwardEntry
        {
            InwardDate = request.InwardDate.Date,
            InwardType = request.InwardType,
            CustomerId = request.CustomerId,
            VendorId = request.VendorId,
            ReferenceInvoiceNo = request.ReferenceInvoiceNo.Trim(),
            ReferenceInvoiceDate = request.ReferenceInvoiceDate,
            ChallanNo = request.ChallanNo.Trim(),
            TransportDetails = request.TransportDetails.Trim(),
            Remarks = request.Remarks.Trim(),
            Status = request.Status,
            InwardNo = await PreviewNextNumberAsync(ct)
        };

        // Reserve the number atomically (Preview only peeks).
        var s = await _settings.GetCompanySettingsAsync(ct);
        var seq = await _uow.Sequences.GetNextAsync("Inward", s.InwardNumberPrefix, DateTime.Today.Year, ct);
        entry.InwardNo = $"{s.InwardNumberPrefix}/{DateTime.Today.Year}/{seq:0000}";

        await _uow.Inwards.AddAsync(entry, ct);
        await BuildItemsAsync(entry, request.Items, ct);
        RecomputeTotals(entry);

        await _uow.SaveChangesAsync(ct);
        await WriteInwardEventsAsync(entry, ItemEventType.InwardReceived, ct);

        await _audit.AddAsync(AuditAction.Create, nameof(InwardEntry), entry.Id,
            $"Created inward {entry.InwardNo} with {entry.Items.Count} item line(s).", ct: ct);

        return OperationResult.Ok($"Inward {entry.InwardNo} created.", new { entry.Id });
    }

    private async Task<OperationResult> UpdateAsync(InwardSaveRequest request, CancellationToken ct)
    {
        var entry = await _uow.Inwards.GetForUpdateAsync(request.Id!.Value, ct);
        if (entry is null || entry.IsDeleted)
            throw new NotFoundException("Inward entry not found.");

        if (entry.Status == InwardStatus.Cancelled)
            throw new BusinessRuleException("A cancelled inward entry cannot be edited.");

        if (await _uow.Inwards.IsDispatchedAsync(entry.Id, ct))
            throw new BusinessRuleException(
                "This inward has already been used to generate a Dispatch Challan. Its items cannot be edited; you may only update the header.");

        entry.InwardDate = request.InwardDate.Date;
        entry.InwardType = request.InwardType;
        entry.CustomerId = request.CustomerId;
        entry.VendorId = request.VendorId;
        entry.ReferenceInvoiceNo = request.ReferenceInvoiceNo.Trim();
        entry.ReferenceInvoiceDate = request.ReferenceInvoiceDate;
        entry.ChallanNo = request.ChallanNo.Trim();
        entry.TransportDetails = request.TransportDetails.Trim();
        entry.Remarks = request.Remarks.Trim();
        entry.Status = request.Status;

        // Soft delete existing serials, then replace line items (tracked collection is
        // diffed by EF; children not referenced by any DC are cascade removed).
        foreach (var existing in entry.Items.ToList())
        {
            foreach (var serial in existing.Serials.ToList())
            {
                serial.IsDeleted = true;
                serial.DeletedOn = DateTime.UtcNow;
                serial.DeletedBy = _currentUser.UserId;
            }
        }
        entry.Items.Clear();

        await BuildItemsAsync(entry, request.Items, ct);
        RecomputeTotals(entry);

        await _uow.SaveChangesAsync(ct);
        await WriteInwardEventsAsync(entry, ItemEventType.Adjustment, ct);

        await _audit.AddAsync(AuditAction.Update, nameof(InwardEntry), entry.Id,
            $"Updated inward {entry.InwardNo}.", ct: ct);

        return OperationResult.Ok($"Inward {entry.InwardNo} updated.", new { entry.Id });
    }

    public async Task<OperationResult> UpdateStatusAsync(InwardStatusRequest request, CancellationToken ct = default)
    {
        var entry = await _uow.Inwards.GetByIdAsync(request.InwardId, ct);
        if (entry is null || entry.IsDeleted)
            throw new NotFoundException("Inward entry not found.");

        if (entry.Status == InwardStatus.Cancelled)
            throw new BusinessRuleException("A cancelled inward entry cannot change status.");

        if (request.Status == InwardStatus.Cancelled && await _uow.Inwards.IsDispatchedAsync(entry.Id, ct))
            throw new BusinessRuleException("An inward with dispatch challans cannot be cancelled.");

        entry.Status = request.Status;
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.Update, nameof(InwardEntry), entry.Id,
            $"Inward {entry.InwardNo} status changed to {request.Status}.", ct: ct);

        return OperationResult.Ok($"Status changed to {request.Status}.");
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await _uow.Inwards.GetForUpdateAsync(id, ct);
        if (entry is null || entry.IsDeleted)
            throw new NotFoundException("Inward entry not found.");

        if (await _uow.Inwards.IsDispatchedAsync(id, ct))
            throw new BusinessRuleException("This inward has dispatch challans and cannot be deleted.");

        foreach (var item in entry.Items)
        {
            foreach (var serial in item.Serials)
            {
                serial.IsDeleted = true;
                serial.DeletedOn = DateTime.UtcNow;
                serial.DeletedBy = _currentUser.UserId;
            }
            item.IsDeleted = true;
            item.DeletedOn = DateTime.UtcNow;
            item.DeletedBy = _currentUser.UserId;
        }

        entry.IsDeleted = true;
        entry.DeletedOn = DateTime.UtcNow;
        entry.DeletedBy = _currentUser.UserId;
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.Delete, nameof(InwardEntry), id,
            $"Deleted inward {entry.InwardNo}.", ct: ct);

        return OperationResult.Ok($"Inward {entry.InwardNo} deleted.");
    }

    private async Task BuildItemsAsync(InwardEntry entry, IReadOnlyList<InwardItemLineRequest> lines, CancellationToken ct)
    {
        var settings = await _settings.GetCompanySettingsAsync(ct);

        foreach (var line in lines)
        {
            var itemEntity = line.ItemId.HasValue ? await _uow.Items.GetByIdAsync(line.ItemId.Value, ct) : null;
            var isSerialTracked = itemEntity?.IsSerialTracked ?? false;

            if (isSerialTracked && settings.RequireSerialForTrackedItems && line.Serials.Count == 0)
                throw new ValidationException($"Serial numbers are required for item '{line.ItemName}'.");

            if (isSerialTracked && line.Serials.Count != (int)line.Quantity)
                throw new ValidationException(
                    $"Item '{line.ItemName}' is serial tracked: the number of serial numbers ({line.Serials.Count}) must equal the quantity ({line.Quantity}).");

            var lineItem = new InwardItem
            {
                ItemId = line.ItemId,
                ItemName = string.IsNullOrWhiteSpace(line.ItemName) ? itemEntity?.Name ?? "" : line.ItemName.Trim(),
                ItemMake = string.IsNullOrWhiteSpace(line.ItemMake) ? itemEntity?.Make ?? "" : line.ItemMake.Trim(),
                ItemModel = string.IsNullOrWhiteSpace(line.ItemModel) ? itemEntity?.Model ?? "" : line.ItemModel.Trim(),
                HsnCode = string.IsNullOrWhiteSpace(line.HsnCode) ? itemEntity?.HsnCode ?? "" : line.HsnCode.Trim(),
                Unit = string.IsNullOrWhiteSpace(line.Unit) ? itemEntity?.Unit ?? "Nos" : line.Unit.Trim(),
                Quantity = line.Quantity,
                Rate = line.Rate,
                Amount = line.Quantity * line.Rate,
                Remarks = line.Remarks.Trim()
            };

            foreach (var serialText in line.Serials)
            {
                var serialNo = serialText.Trim();
                if (string.IsNullOrWhiteSpace(serialNo)) continue;

                if (await _uow.SerialNumbers.SerialExistsAsync(serialNo, ct))
                    throw new DuplicateException($"Serial number '{serialNo}' already exists in the system.");

                var serial = new SerialNumber
                {
                    ItemId = line.ItemId ?? Guid.Empty,
                    SerialNo = serialNo,
                    Status = SerialStatus.InStock,
                    InwardEntryId = entry.Id,
                    Notes = line.Remarks.Trim()
                };
                lineItem.Serials.Add(serial);
            }

            entry.Items.Add(lineItem);
        }

        if (entry.Items.Count == 0)
            throw new ValidationException("At least one item line is required.");
    }

    private async Task WriteInwardEventsAsync(InwardEntry entry, ItemEventType type, CancellationToken ct)
    {
        foreach (var line in entry.Items)
        {
            if (!line.ItemId.HasValue) continue;

            if (line.Serials.Count > 0)
            {
                foreach (var serial in line.Serials)
                {
                    await _uow.ItemEvents.AddAsync(new ItemEvent
                    {
                        ItemId = line.ItemId.Value,
                        SerialNo = serial.SerialNo,
                        EventType = type,
                        ReferenceType = nameof(InwardEntry),
                        ReferenceId = entry.Id,
                        ReferenceNumber = entry.InwardNo,
                        Quantity = 1,
                        Notes = line.ItemName,
                        EventedBy = _currentUser.UserId,
                        EventedOn = DateTime.UtcNow
                    }, ct);
                }
            }
            else
            {
                await _uow.ItemEvents.AddAsync(new ItemEvent
                {
                    ItemId = line.ItemId.Value,
                    EventType = type,
                    ReferenceType = nameof(InwardEntry),
                    ReferenceId = entry.Id,
                    ReferenceNumber = entry.InwardNo,
                    Quantity = line.Quantity,
                    Notes = line.ItemName,
                    EventedBy = _currentUser.UserId,
                    EventedOn = DateTime.UtcNow
                }, ct);
            }
        }

        await _uow.SaveChangesAsync(ct);
    }

    private static void RecomputeTotals(InwardEntry entry)
    {
        entry.TotalQuantity = entry.Items.Sum(i => i.Quantity);
        entry.TotalAmount = entry.Items.Sum(i => i.Amount);
    }

    private static void Validate(InwardSaveRequest request)
    {
        var errors = new List<string>();
        if (request.Items.Count == 0)
            errors.Add("At least one item line is required.");

        if (request.InwardType == InwardType.CustomerReturn && !request.CustomerId.HasValue)
            errors.Add("Customer is required for a customer return inward.");

        if (request.InwardType == InwardType.Purchase && !request.VendorId.HasValue)
            errors.Add("Vendor is required for a purchase inward.");

        foreach (var line in request.Items)
        {
            if (line.Quantity <= 0)
                errors.Add($"Quantity must be greater than zero for '{line.ItemName}'.");
            if (line.Rate < 0)
                errors.Add($"Rate cannot be negative for '{line.ItemName}'.");
        }

        if (errors.Count > 0) throw new ValidationException(errors);
    }

    internal static InwardDto ToDto(InwardEntry x, bool includeItems)
    {
        var dto = new InwardDto
        {
            Id = x.Id,
            InwardNo = x.InwardNo,
            InwardDate = x.InwardDate,
            InwardType = x.InwardType,
            CustomerId = x.CustomerId,
            CustomerName = x.Customer?.Name ?? string.Empty,
            VendorId = x.VendorId,
            VendorName = x.Vendor?.Name ?? string.Empty,
            ReferenceInvoiceNo = x.ReferenceInvoiceNo,
            ReferenceInvoiceDate = x.ReferenceInvoiceDate,
            ChallanNo = x.ChallanNo,
            TransportDetails = x.TransportDetails,
            Remarks = x.Remarks,
            Status = x.Status,
            TotalQuantity = x.TotalQuantity,
            TotalAmount = x.TotalAmount,
            CreatedOn = x.CreatedOn
        };

        if (includeItems)
        {
            dto.Items = x.Items.Select(i => new InwardItemDto
            {
                Id = i.Id,
                ItemId = i.ItemId,
                ItemName = i.ItemName,
                ItemMake = i.ItemMake,
                ItemModel = i.ItemModel,
                HsnCode = i.HsnCode,
                Unit = i.Unit,
                Quantity = i.Quantity,
                Rate = i.Rate,
                Amount = i.Amount,
                DispatchedQuantity = i.DispatchedQuantity,
                Remarks = i.Remarks,
                Serials = i.Serials.Where(s => !s.IsDeleted).Select(s => s.SerialNo).ToList()
            }).ToList();
            dto.Attachments = x.Attachments.Select(a => new AttachmentDto
            {
                Id = a.Id,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                FileName = a.FileName,
                StoredPath = a.StoredPath,
                ContentType = a.ContentType,
                FileSize = a.FileSize,
                Notes = a.Notes,
                UploadedOn = a.UploadedOn
            }).ToList();
        }

        return dto;
    }
}
