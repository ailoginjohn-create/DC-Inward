using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Interfaces;

namespace InwardDC.Application.Services;

/// <summary>
/// Report engine producing daily, monthly, customer-wise and item-wise summaries
/// from inward and dispatch data.
/// </summary>
public class ReportService : IReportService
{
    private readonly IUnitOfWork _uow;

    public ReportService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyList<DailySummaryDto>> GetDailySummaryAsync(ReportPeriodFilter filter, CancellationToken ct = default)
    {
        var inwards = (await _uow.Inwards.GetByPeriodAsync(filter.FromDate, filter.ToDate, ct))
            .Where(x => filter.IncludeCancelled || x.Status != InwardStatus.Cancelled);
        var dcs = (await _uow.DCs.GetByPeriodAsync(filter.FromDate, filter.ToDate, ct))
            .Where(x => filter.IncludeCancelled || x.Status != DispatchStatus.Cancelled);

        var days = inwards.Select(x => x.InwardDate.Date)
            .Union(dcs.Select(x => x.DcDate.Date))
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        return days.Select(day => new DailySummaryDto
        {
            Date = day,
            InwardCount = inwards.Count(x => x.InwardDate.Date == day),
            InwardAmount = inwards.Where(x => x.InwardDate.Date == day).Sum(x => x.TotalAmount),
            DcCount = dcs.Count(x => x.DcDate.Date == day),
            DcAmount = dcs.Where(x => x.DcDate.Date == day).Sum(x => x.TotalAmount)
        }).ToList();
    }

    public async Task<IReadOnlyList<MonthlySummaryDto>> GetMonthlySummaryAsync(ReportPeriodFilter filter, CancellationToken ct = default)
    {
        var inwards = (await _uow.Inwards.GetByPeriodAsync(filter.FromDate, filter.ToDate, ct))
            .Where(x => filter.IncludeCancelled || x.Status != InwardStatus.Cancelled);
        var dcs = (await _uow.DCs.GetByPeriodAsync(filter.FromDate, filter.ToDate, ct))
            .Where(x => filter.IncludeCancelled || x.Status != DispatchStatus.Cancelled);

        var months = inwards.Select(x => x.InwardDate.ToString("yyyy-MM"))
            .Union(dcs.Select(x => x.DcDate.ToString("yyyy-MM")))
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        return months.Select(m => new MonthlySummaryDto
        {
            YearMonth = m,
            InwardCount = inwards.Count(x => x.InwardDate.ToString("yyyy-MM") == m),
            InwardAmount = inwards.Where(x => x.InwardDate.ToString("yyyy-MM") == m).Sum(x => x.TotalAmount),
            DcCount = dcs.Count(x => x.DcDate.ToString("yyyy-MM") == m),
            DcAmount = dcs.Where(x => x.DcDate.ToString("yyyy-MM") == m).Sum(x => x.TotalAmount)
        }).ToList();
    }

    public async Task<IReadOnlyList<CustomerWiseSummaryDto>> GetCustomerWiseAsync(ReportPeriodFilter filter, CancellationToken ct = default)
    {
        var inwards = (await _uow.Inwards.GetByPeriodAsync(filter.FromDate, filter.ToDate, ct))
            .Where(x => filter.IncludeCancelled || x.Status != InwardStatus.Cancelled)
            .Where(x => !filter.CustomerId.HasValue || x.CustomerId == filter.CustomerId.Value);
        var dcs = (await _uow.DCs.GetByPeriodAsync(filter.FromDate, filter.ToDate, ct))
            .Where(x => filter.IncludeCancelled || x.Status != DispatchStatus.Cancelled)
            .Where(x => !filter.CustomerId.HasValue || x.CustomerId == filter.CustomerId.Value);

        var names = inwards.Select(x => x.Customer?.Name ?? x.Vendor?.Name ?? "Unknown")
            .Union(dcs.Select(x => x.Customer?.Name ?? "Unknown"))
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        return names.Select(name => new CustomerWiseSummaryDto
        {
            PartyName = name,
            InwardCount = inwards.Count(x => (x.Customer?.Name ?? x.Vendor?.Name ?? "Unknown") == name),
            InwardUnits = (int)inwards.Where(x => (x.Customer?.Name ?? x.Vendor?.Name ?? "Unknown") == name).Sum(x => x.TotalQuantity),
            InwardAmount = inwards.Where(x => (x.Customer?.Name ?? x.Vendor?.Name ?? "Unknown") == name).Sum(x => x.TotalAmount),
            DcCount = dcs.Count(x => (x.Customer?.Name ?? "Unknown") == name),
            DcUnits = (int)dcs.Where(x => (x.Customer?.Name ?? "Unknown") == name).Sum(x => x.TotalQuantity),
            DcAmount = dcs.Where(x => (x.Customer?.Name ?? "Unknown") == name).Sum(x => x.TotalAmount)
        }).ToList();
    }

    public async Task<IReadOnlyList<ItemWiseSummaryDto>> GetItemWiseAsync(ReportPeriodFilter filter, CancellationToken ct = default)
    {
        var inwards = await _uow.Inwards.GetByPeriodDetailedAsync(filter.FromDate, filter.ToDate, ct);
        var dcs = await _uow.DCs.GetByPeriodDetailedAsync(filter.FromDate, filter.ToDate, ct);

        var inwardLines = inwards
            .Where(x => filter.IncludeCancelled || x.Status != InwardStatus.Cancelled)
            .SelectMany(x => x.Items.Where(i => !i.IsDeleted))
            .Where(i => !filter.ItemId.HasValue || i.ItemId == filter.ItemId.Value)
            .ToList();

        var dcLines = dcs
            .Where(x => filter.IncludeCancelled || x.Status != DispatchStatus.Cancelled)
            .SelectMany(x => x.Items.Where(i => !i.IsDeleted))
            .Where(i => !filter.ItemId.HasValue || i.ItemId == filter.ItemId.Value)
            .ToList();

        var keys = inwardLines.Select(i => (i.ItemId, i.ItemName, i.ItemMake, i.ItemModel))
            .Union(dcLines.Select(i => (i.ItemId, i.ItemName, i.ItemMake, i.ItemModel)))
            .Distinct()
            .OrderBy(k => k.ItemName)
            .ToList();

        var stock = await _uow.SerialNumbers.GetAllWithDetailsAsync(null, null, ct);

        return keys.Select(k => new ItemWiseSummaryDto
        {
            ItemName = k.ItemName,
            Make = k.ItemMake,
            Model = k.ItemModel,
            InwardUnits = (int)inwardLines.Where(i => i.ItemName == k.ItemName && i.ItemMake == k.ItemMake && i.ItemModel == k.ItemModel).Sum(i => i.Quantity),
            InwardAmount = inwardLines.Where(i => i.ItemName == k.ItemName && i.ItemMake == k.ItemMake && i.ItemModel == k.ItemModel).Sum(i => i.Amount),
            DispatchedUnits = (int)dcLines.Where(i => i.ItemName == k.ItemName && i.ItemMake == k.ItemMake && i.ItemModel == k.ItemModel).Sum(i => i.Quantity),
            DispatchedAmount = dcLines.Where(i => i.ItemName == k.ItemName && i.ItemMake == k.ItemMake && i.ItemModel == k.ItemModel).Sum(i => i.Amount),
            InStock = k.ItemId.HasValue ? stock.Count(s => s.ItemId == k.ItemId.Value && s.Status == SerialStatus.InStock) : 0
        }).ToList();
    }

    public async Task<IReadOnlyList<ReportRowDto>> GetInwardDetailAsync(ReportPeriodFilter filter, CancellationToken ct = default)
    {
        var inwards = await _uow.Inwards.GetByPeriodDetailedAsync(filter.FromDate, filter.ToDate, ct);

        return inwards
            .Where(x => filter.IncludeCancelled || x.Status != InwardStatus.Cancelled)
            .Where(x => !filter.CustomerId.HasValue || x.CustomerId == filter.CustomerId.Value)
            .SelectMany(x => x.Items.Where(i => !i.IsDeleted), (x, i) => new ReportRowDto
            {
                Date = x.InwardDate,
                Number = x.InwardNo,
                Type = $"Inward - {x.InwardType}",
                Party = x.Customer?.Name ?? x.Vendor?.Name ?? string.Empty,
                ItemName = i.ItemName,
                SerialNo = string.Join(", ", i.Serials.Where(s => !s.IsDeleted).Select(s => s.SerialNo)),
                Quantity = i.Quantity,
                Unit = i.Unit,
                Rate = i.Rate,
                Amount = i.Amount,
                Status = x.Status.ToString()
            })
            .OrderBy(r => r.Date)
            .ToList();
    }

    public async Task<IReadOnlyList<ReportRowDto>> GetDispatchDetailAsync(ReportPeriodFilter filter, CancellationToken ct = default)
    {
        var dcs = await _uow.DCs.GetByPeriodDetailedAsync(filter.FromDate, filter.ToDate, ct);

        return dcs
            .Where(x => filter.IncludeCancelled || x.Status != DispatchStatus.Cancelled)
            .Where(x => !filter.CustomerId.HasValue || x.CustomerId == filter.CustomerId.Value)
            .SelectMany(x => x.Items.Where(i => !i.IsDeleted), (x, i) => new ReportRowDto
            {
                Date = x.DcDate,
                Number = x.DcNo,
                Type = "Dispatch Challan",
                Party = x.Customer?.Name ?? string.Empty,
                ItemName = i.ItemName,
                SerialNo = string.Join(", ", i.Serials.Where(s => !s.IsDeleted).Select(s => s.SerialNo)),
                Quantity = i.Quantity,
                Unit = i.Unit,
                Rate = i.Rate,
                Amount = i.Amount,
                Status = x.Status.ToString()
            })
            .OrderBy(r => r.Date)
            .ToList();
    }
}
