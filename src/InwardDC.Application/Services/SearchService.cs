using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Interfaces;

namespace InwardDC.Application.Services;

/// <summary>
/// Powerful search across customers, items, serial numbers, models, dates, DCs,
/// invoices, challans and statuses, plus the item history timeline and stock view.
/// </summary>
public class SearchService : ISearchService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public SearchService(IUnitOfWork uow, ICurrentUserService currentUser, IAuditService audit)
    {
        _uow = uow;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<SearchResultDto> GlobalSearchAsync(GlobalSearchFilter filter, CancellationToken ct = default)
    {
        var query = filter.Query?.Trim() ?? string.Empty;
        var result = new SearchResultDto();

        // Customers
        var customers = await _uow.Customers.GetPagedAsync(new CustomerSearchFilter
        {
            SearchText = query,
            PageSize = 10
        }, ct);
        result.Customers = customers.Items.Select(c => new SearchHitDto
        {
            Module = "Customer",
            EntityType = nameof(Customer),
            EntityId = c.Id,
            Title = c.Name,
            Subtitle = $"{c.Code} | {c.ContactPerson} | {c.Mobile} | {c.City}",
            Status = c.IsActive ? "Active" : "Inactive"
        }).ToList();

        // Items (match by code/name/make/model/serial)
        var items = await _uow.Items.GetPagedAsync(new ItemSearchFilter { SearchText = query, PageSize = 10 }, ct);
        result.Items = items.Items.Select(i => new SearchHitDto
        {
            Module = "Item",
            EntityType = nameof(Item),
            EntityId = i.Id,
            Title = i.Name,
            Subtitle = $"{i.Code} | {i.Make} {i.Model}".Trim(),
            Status = i.IsSerialTracked ? "Serial tracked" : "Quantity"
        }).ToList();

        // Serial lookup expands to its owning item
        if (!string.IsNullOrWhiteSpace(query))
        {
            var serialItem = await _uow.Items.GetBySerialAsync(query, ct);
            if (serialItem is not null)
            {
                result.Items = result.Items.Concat(new[]
                {
                    new SearchHitDto
                    {
                        Module = "Item",
                        EntityType = nameof(Item),
                        EntityId = serialItem.Id,
                        Title = $"{serialItem.Name} (serial {query})",
                        Subtitle = $"{serialItem.Code} | {serialItem.Make} {serialItem.Model}".Trim()
                    }
                }).ToList();
            }
        }

        // Inward entries
        var inwards = await _uow.Inwards.GetPagedAsync(new InwardSearchFilter
        {
            SearchText = query,
            SerialNumber = query,
            Model = query,
            InvoiceNo = query,
            ChallanNo = query,
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            Status = ParseInwardStatus(filter.Status),
            PageSize = 10
        }, ct);
        result.Inwards = inwards.Items.Select(x => new SearchHitDto
        {
            Module = "Inward",
            EntityType = nameof(InwardEntry),
            EntityId = x.Id,
            Title = x.InwardNo,
            Subtitle = $"{x.InwardDate:d} | {(x.Customer?.Name ?? x.Vendor?.Name ?? "")} | {x.ReferenceInvoiceNo}",
            ReferenceNumber = x.InwardNo,
            Date = x.InwardDate,
            Status = x.Status.ToString()
        }).ToList();

        // Dispatch challans
        var dcs = await _uow.DCs.GetPagedAsync(new DispatchSearchFilter
        {
            SearchText = query,
            SerialNumber = query,
            Model = query,
            InvoiceNo = query,
            ChallanNo = query,
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            Status = ParseDispatchStatus(filter.Status),
            PageSize = 10
        }, ct);
        result.Dispatches = dcs.Items.Select(x => new SearchHitDto
        {
            Module = "Dispatch Challan",
            EntityType = nameof(DispatchChallan),
            EntityId = x.Id,
            Title = x.DcNo,
            Subtitle = $"{x.DcDate:d} | {x.Customer?.Name ?? ""} | {x.SourceInwardEntry?.InwardNo}",
            ReferenceNumber = x.DcNo,
            Date = x.DcDate,
            Status = x.Status.ToString()
        }).ToList();

        await _audit.AddAsync(AuditAction.Search, "GlobalSearch", null,
            $"Searched for '{query}' across all modules.", ct: ct);

        return result;
    }

    public async Task<IReadOnlyList<ItemHistoryDto>> GetItemHistoryAsync(Guid itemId, CancellationToken ct = default)
    {
        var events = await _uow.ItemEvents.GetByItemAsync(itemId, ct);
        return await MapEventsAsync(events, ct);
    }

    public async Task<IReadOnlyList<ItemHistoryDto>> GetSerialHistoryAsync(string serialNo, CancellationToken ct = default)
    {
        var events = await _uow.ItemEvents.GetBySerialAsync(serialNo, ct);
        return await MapEventsAsync(events, ct);
    }

    private async Task<IReadOnlyList<ItemHistoryDto>> MapEventsAsync(IReadOnlyList<ItemEvent> events, CancellationToken ct)
    {
        var users = await _uow.Users.GetAllAsync(ct);
        var userMap = users.ToDictionary(u => u.Id, u => u.FullName);

        return events.Select(e => new ItemHistoryDto
        {
            EventedOn = e.EventedOn,
            EventType = e.EventType.ToString(),
            SerialNo = e.SerialNo,
            ReferenceType = e.ReferenceType,
            ReferenceNumber = e.ReferenceNumber,
            Quantity = e.Quantity,
            Notes = e.Notes,
            UserName = e.EventedBy.HasValue && userMap.TryGetValue(e.EventedBy.Value, out var name) ? name : string.Empty
        }).ToList();
    }

    public async Task<IReadOnlyList<ItemStockDto>> GetStockReportAsync(Guid? itemId = null, string? search = null, CancellationToken ct = default)
    {
        var serials = await _uow.SerialNumbers.GetAllWithDetailsAsync(itemId, search, ct);

        return serials.Select(s => new ItemStockDto
        {
            ItemId = s.ItemId,
            ItemName = s.Item?.Name ?? string.Empty,
            Make = s.Item?.Make ?? string.Empty,
            Model = s.Item?.Model ?? string.Empty,
            SerialNo = s.SerialNo,
            Status = s.Status,
            InwardNo = s.InwardEntry?.InwardNo ?? string.Empty,
            DcNo = s.DispatchChallan?.DcNo ?? string.Empty,
            DispatchedOn = s.DispatchedOn,
            CustomerName = s.DispatchChallan?.Customer?.Name ?? string.Empty
        }).ToList();
    }

    public async Task<IReadOnlyList<ItemStockDto>> GetSerialLookupAsync(string serialNo, CancellationToken ct = default)
    {
        var serial = await _uow.SerialNumbers.GetBySerialAsync(serialNo.Trim(), ct);
        if (serial is null)
            return Array.Empty<ItemStockDto>();

        return await GetStockReportAsync(serial.ItemId, serialNo, ct);
    }

    private static InwardStatus? ParseInwardStatus(string? status)
        => Enum.TryParse<InwardStatus>(status, ignoreCase: true, out var s) ? s : null;

    private static DispatchStatus? ParseDispatchStatus(string? status)
        => Enum.TryParse<DispatchStatus>(status, ignoreCase: true, out var s) ? s : null;
}
