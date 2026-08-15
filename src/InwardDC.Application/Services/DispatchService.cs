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

/// <summary>
/// Dispatch Challan business logic. Creating a DC atomically:
///   1. reserves the DC number,
///   2. allocates in-stock serial numbers / quantities from inward lines,
///   3. marks serials as Dispatched,
///   4. rolls the source inward entry status forward (Partial / Full dispatch).
/// Cancelling reverses every step so stock always reconciles.
/// </summary>
public class DispatchService : IDispatchService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly ISettingsService _settings;
    private readonly ILogger<DispatchService> _logger;

    public DispatchService(IUnitOfWork uow, ICurrentUserService currentUser, IAuditService audit,
        ISettingsService settings, ILogger<DispatchService> logger)
    {
        _uow = uow;
        _currentUser = currentUser;
        _audit = audit;
        _settings = settings;
        _logger = logger;
    }

    public async Task<PagedResponse<DispatchDto>> GetPagedAsync(DispatchSearchFilter filter, CancellationToken ct = default)
    {
        var result = await _uow.DCs.GetPagedAsync(filter, ct);
        var page = new PagedResult<DispatchDto>
        {
            Items = result.Items.Select(x => ToDto(x, includeItems: false)).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
        return PagedResponse<DispatchDto>.From(page);
    }

    public async Task<DispatchDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var dc = await _uow.DCs.GetByIdAsync(id, ct);
        return dc is null ? null : ToDto(dc, includeItems: true);
    }

    public async Task<IReadOnlyList<AvailableStockDto>> GetAvailableStockAsync(Guid? itemId = null, string? search = null, CancellationToken ct = default)
    {
        var stock = await _uow.Inwards.GetAvailableStockAsync(itemId, search, ct);
        return stock.Select(x => new AvailableStockDto
        {
            InwardItemId = x.Id,
            InwardNo = x.InwardEntry?.InwardNo ?? string.Empty,
            ItemId = x.ItemId ?? Guid.Empty,
            ItemName = x.ItemName,
            Make = x.ItemMake,
            Model = x.ItemModel,
            Unit = x.Unit,
            AvailableQuantity = x.Quantity - x.DispatchedQuantity,
            Rate = x.Rate,
            AvailableSerials = x.Serials.Select(s => s.SerialNo).ToList()
        }).Where(x => x.AvailableQuantity > 0).ToList();
    }

    public async Task<string> PreviewNextNumberAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetCompanySettingsAsync(ct);
        var year = DateTime.Today.Year;
        var counter = await _uow.Sequences.GetCurrentAsync("DC", s.DcNumberPrefix, year, ct);
        var next = counter?.LastNumber + 1 ?? 1;
        return $"{s.DcNumberPrefix}/{year}/{next:0000}";
    }

    public async Task<OperationResult> SaveAsync(DispatchSaveRequest request, CancellationToken ct = default)
    {
        Validate(request);

        if (request.Id.HasValue)
            throw new BusinessRuleException(
                "Editing a Dispatch Challan is not supported. Cancel it and create a new one.");

        var s = await _settings.GetCompanySettingsAsync(ct);
        var seq = await _uow.Sequences.GetNextAsync("DC", s.DcNumberPrefix, DateTime.Today.Year, ct);
        var dcNo = $"{s.DcNumberPrefix}/{DateTime.Today.Year}/{seq:0000}";

        var dc = new DispatchChallan
        {
            DcNo = dcNo,
            DcDate = request.DcDate.Date,
            CustomerId = request.CustomerId,
            PurposeId = request.PurposeId,
            SourceInwardEntryId = request.SourceInwardEntryId,
            ReferenceChallanNo = request.ReferenceChallanNo.Trim(),
            InvoiceNo = request.InvoiceNo.Trim(),
            TransportDetails = request.TransportDetails.Trim(),
            PaymentStatus = request.PaymentStatus.Trim(),
            ModeOfDispatch = request.ModeOfDispatch.Trim(),
            PodNo = request.PodNo.Trim(),
            Remarks = request.Remarks.Trim(),
            Status = DispatchStatus.Generated
        };

        await _uow.DCs.AddAsync(dc, ct);

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            var affectedInwardIds = new HashSet<Guid>();

            foreach (var line in request.Items)
            {
                if (!line.SourceInwardItemId.HasValue)
                    throw new ValidationException($"Source inward line is missing for item '{line.ItemName}'.");

                var inwardItem = await _uow.Inwards.GetInwardItemForUpdateAsync(line.SourceInwardItemId.Value, ct);
                if (inwardItem is null || inwardItem.InwardEntry is null || inwardItem.InwardEntry.IsDeleted)
                    throw new NotFoundException($"Source inward line not found for item '{line.ItemName}'.");

                if (inwardItem.InwardEntry.Status == InwardStatus.Cancelled)
                    throw new BusinessRuleException($"Inward {inwardItem.InwardEntry.InwardNo} is cancelled and cannot be dispatched.");

                var available = inwardItem.Quantity - inwardItem.DispatchedQuantity;
                if (line.Quantity <= 0 || line.Quantity > available)
                    throw new BusinessRuleException(
                        $"Only {available:0.###} unit(s) available for '{line.ItemName}' (inward {inwardItem.InwardEntry.InwardNo}).");

                var isSerialTracked = inwardItem.Item?.IsSerialTracked ?? false;

                if (isSerialTracked)
                {
                    if (line.Serials.Count == 0)
                        throw new ValidationException($"Select serial numbers to dispatch for '{line.ItemName}'.");
                    if (line.Serials.Count != (int)line.Quantity)
                        throw new ValidationException(
                            $"Number of selected serials ({line.Serials.Count}) must equal quantity ({line.Quantity}) for '{line.ItemName}'.");
                }

                var dispatchItem = new DispatchItem
                {
                    DispatchChallanId = dc.Id,
                    SourceInwardItemId = inwardItem.Id,
                    ItemId = inwardItem.ItemId,
                    ItemName = line.ItemName,
                    ItemMake = line.ItemMake,
                    ItemModel = line.ItemModel,
                    HsnCode = line.HsnCode,
                    Unit = line.Unit,
                    Quantity = line.Quantity,
                    Rate = line.Rate,
                    Amount = line.Quantity * line.Rate,
                    Remarks = line.Remarks.Trim()
                };

                // Allocate serials (only in-stock serials belonging to this inward line).
                var inStockSerials = inwardItem.Serials.Where(x => x.Status == SerialStatus.InStock).ToList();
                foreach (var serialText in line.Serials)
                {
                    var serialNo = serialText.Trim();
                    var serial = inStockSerials.FirstOrDefault(x => x.SerialNo == serialNo);
                    if (serial is null)
                        throw new ValidationException($"Serial '{serialNo}' is not available for dispatch on '{line.ItemName}'.");

                    serial.Status = SerialStatus.Dispatched;
                    serial.DispatchChallanId = dc.Id;
                    serial.DispatchItemId = dispatchItem.Id;
                    serial.DispatchedOn = DateTime.UtcNow;
                    dispatchItem.Serials.Add(serial);

                    await _uow.ItemEvents.AddAsync(new ItemEvent
                    {
                        ItemId = inwardItem.ItemId ?? Guid.Empty,
                        SerialNo = serialNo,
                        EventType = ItemEventType.Dispatched,
                        ReferenceType = nameof(DispatchChallan),
                        ReferenceId = dc.Id,
                        ReferenceNumber = dc.DcNo,
                        Quantity = 1,
                        Notes = line.ItemName,
                        EventedBy = _currentUser.UserId,
                        EventedOn = DateTime.UtcNow
                    }, ct);
                }

                if (!isSerialTracked)
                {
                    await _uow.ItemEvents.AddAsync(new ItemEvent
                    {
                        ItemId = inwardItem.ItemId ?? Guid.Empty,
                        EventType = ItemEventType.Dispatched,
                        ReferenceType = nameof(DispatchChallan),
                        ReferenceId = dc.Id,
                        ReferenceNumber = dc.DcNo,
                        Quantity = line.Quantity,
                        Notes = line.ItemName,
                        EventedBy = _currentUser.UserId,
                        EventedOn = DateTime.UtcNow
                    }, ct);
                }

                inwardItem.DispatchedQuantity += line.Quantity;
                affectedInwardIds.Add(inwardItem.InwardEntryId);
                dc.Items.Add(dispatchItem);
            }

            foreach (var inwardId in affectedInwardIds)
                await RecomputeInwardStatusAsync(inwardId, ct);

            dc.TotalQuantity = dc.Items.Sum(i => i.Quantity);
            dc.TotalAmount = dc.Items.Sum(i => i.Amount);

            await _uow.SaveChangesAsync(ct);
        }, ct);

        await _audit.AddAsync(AuditAction.GenerateDC, nameof(DispatchChallan), dc.Id,
            $"Generated Dispatch Challan {dc.DcNo} for {dc.Items.Count} item line(s).", ct: ct);

        return OperationResult.Ok($"Dispatch Challan {dc.DcNo} generated.", new { dc.Id, dc.DcNo });
    }

    public async Task<OperationResult> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var dc = await _uow.DCs.GetForUpdateAsync(id, ct);
        if (dc is null || dc.IsDeleted)
            throw new NotFoundException("Dispatch Challan not found.");

        if (dc.Status == DispatchStatus.Cancelled)
            throw new BusinessRuleException("This Dispatch Challan is already cancelled.");

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            var affectedInwardIds = new HashSet<Guid>();

            foreach (var item in dc.Items)
            {
                foreach (var serial in item.Serials.ToList())
                {
                    serial.Status = SerialStatus.InStock;
                    serial.DispatchChallanId = null;
                    serial.DispatchItemId = null;
                    serial.DispatchedOn = null;

                    await _uow.ItemEvents.AddAsync(new ItemEvent
                    {
                        ItemId = item.ItemId ?? Guid.Empty,
                        SerialNo = serial.SerialNo,
                        EventType = ItemEventType.DispatchCancelled,
                        ReferenceType = nameof(DispatchChallan),
                        ReferenceId = dc.Id,
                        ReferenceNumber = dc.DcNo,
                        Quantity = 1,
                        Notes = item.ItemName,
                        EventedBy = _currentUser.UserId,
                        EventedOn = DateTime.UtcNow
                    }, ct);
                }

                if (item.SourceInwardItem is not null)
                {
                    item.SourceInwardItem.DispatchedQuantity -= item.Quantity;
                    affectedInwardIds.Add(item.SourceInwardItem.InwardEntryId);
                }

                if (item.SourceInwardItem is null && item.ItemId.HasValue)
                {
                    await _uow.ItemEvents.AddAsync(new ItemEvent
                    {
                        ItemId = item.ItemId.Value,
                        EventType = ItemEventType.DispatchCancelled,
                        ReferenceType = nameof(DispatchChallan),
                        ReferenceId = dc.Id,
                        ReferenceNumber = dc.DcNo,
                        Quantity = item.Quantity,
                        Notes = item.ItemName,
                        EventedBy = _currentUser.UserId,
                        EventedOn = DateTime.UtcNow
                    }, ct);
                }
            }

            dc.Status = DispatchStatus.Cancelled;

            foreach (var inwardId in affectedInwardIds)
                await RecomputeInwardStatusAsync(inwardId, ct);

            await _uow.SaveChangesAsync(ct);
        }, ct);

        await _audit.AddAsync(AuditAction.CancelDC, nameof(DispatchChallan), dc.Id,
            $"Cancelled Dispatch Challan {dc.DcNo}.", ct: ct);

        return OperationResult.Ok($"Dispatch Challan {dc.DcNo} cancelled.");
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var dc = await _uow.DCs.GetForUpdateAsync(id, ct);
        if (dc is null || dc.IsDeleted)
            throw new NotFoundException("Dispatch Challan not found.");

        if (dc.Status != DispatchStatus.Cancelled)
            await CancelAsync(id, ct);

        foreach (var item in dc.Items)
            item.IsDeleted = true;

        dc.IsDeleted = true;
        dc.DeletedOn = DateTime.UtcNow;
        dc.DeletedBy = _currentUser.UserId;
        await _uow.SaveChangesAsync(ct);

        await _audit.AddAsync(AuditAction.Delete, nameof(DispatchChallan), dc.Id,
            $"Deleted Dispatch Challan {dc.DcNo}.", ct: ct);

        return OperationResult.Ok($"Dispatch Challan {dc.DcNo} deleted.");
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

    private static void Validate(DispatchSaveRequest request)
    {
        var errors = new List<string>();
        if (request.CustomerId == Guid.Empty)
            errors.Add("Customer is required.");

        if (request.Items.Count == 0)
            errors.Add("At least one dispatch line is required.");

        foreach (var line in request.Items)
        {
            if (line.Quantity <= 0)
                errors.Add($"Quantity must be greater than zero for '{line.ItemName}'.");
            if (!line.SourceInwardItemId.HasValue)
                errors.Add($"Select a source inward line for '{line.ItemName}'.");
        }

        if (errors.Count > 0) throw new ValidationException(errors);
    }

    internal static DispatchDto ToDto(DispatchChallan x, bool includeItems)
    {
        var dto = new DispatchDto
        {
            Id = x.Id,
            DcNo = x.DcNo,
            DcDate = x.DcDate,
            CustomerId = x.CustomerId,
            CustomerName = x.Customer?.Name ?? string.Empty,
            PurposeId = x.PurposeId,
            PurposeName = x.Purpose?.Name ?? string.Empty,
            SourceInwardEntryId = x.SourceInwardEntryId,
            SourceInwardNo = x.SourceInwardEntry?.InwardNo ?? string.Empty,
            ReferenceChallanNo = x.ReferenceChallanNo,
            InvoiceNo = x.InvoiceNo,
            TransportDetails = x.TransportDetails,
            PaymentStatus = x.PaymentStatus,
            ModeOfDispatch = x.ModeOfDispatch,
            PodNo = x.PodNo,
            Remarks = x.Remarks,
            Status = x.Status,
            TotalQuantity = x.TotalQuantity,
            TotalAmount = x.TotalAmount,
            CreatedOn = x.CreatedOn
        };

        if (includeItems)
        {
            dto.Items = x.Items.Select(i => new DispatchItemDto
            {
                Id = i.Id,
                SourceInwardItemId = i.SourceInwardItemId,
                SourceInwardNo = i.SourceInwardItem?.InwardEntry?.InwardNo ?? string.Empty,
                ItemId = i.ItemId,
                ItemName = i.ItemName,
                ItemMake = i.ItemMake,
                ItemModel = i.ItemModel,
                HsnCode = i.HsnCode,
                Unit = i.Unit,
                Quantity = i.Quantity,
                Rate = i.Rate,
                Amount = i.Amount,
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
