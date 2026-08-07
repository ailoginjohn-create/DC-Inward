using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Criteria;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Interfaces;

namespace InwardDC.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _uow;

    public DashboardService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        var customers = await _uow.Customers.CountAsync(ct);
        var vendors = await _uow.Vendors.CountAsync(ct);
        var items = await _uow.Items.CountAsync(ct);
        var inwards = await _uow.Inwards.CountAsync(ct);
        var dcs = await _uow.DCs.CountAsync(ct);
        var inStock = await _uow.SerialNumbers.CountInStockAsync(ct);

        var monthInwards = await _uow.Inwards.GetPagedAsync(new InwardSearchFilter
        {
            FromDate = monthStart,
            ToDate = DateTime.Today,
            Page = 1,
            PageSize = 1
        }, ct);

        var monthDcs = await _uow.DCs.GetPagedAsync(new DispatchSearchFilter
        {
            FromDate = monthStart,
            ToDate = DateTime.Today,
            Page = 1,
            PageSize = 1
        }, ct);

        var pending = await _uow.Inwards.GetPagedAsync(new InwardSearchFilter
        {
            Status = InwardStatus.PartiallyDispatched,
            Page = 1,
            PageSize = 1
        }, ct);

        var pendingReceived = await _uow.Inwards.GetPagedAsync(new InwardSearchFilter
        {
            Status = InwardStatus.Received,
            Page = 1,
            PageSize = 1
        }, ct);

        var recentInwards = await _uow.Inwards.GetPagedAsync(new InwardSearchFilter { Page = 1, PageSize = 6 }, ct);
        var recentDcs = await _uow.DCs.GetPagedAsync(new DispatchSearchFilter { Page = 1, PageSize = 6 }, ct);

        var monthAmount = 0m;
        var dcAmount = 0m;
        var monthlyEntries = await _uow.Inwards.GetByPeriodAsync(monthStart, DateTime.Today, ct);
        var monthlyDcEntries = await _uow.DCs.GetByPeriodAsync(monthStart, DateTime.Today, ct);
        monthAmount = monthlyEntries.Where(x => x.Status != InwardStatus.Cancelled).Sum(x => x.TotalAmount);
        dcAmount = monthlyDcEntries.Where(x => x.Status != DispatchStatus.Cancelled).Sum(x => x.TotalAmount);

        return new DashboardStatsDto
        {
            TotalCustomers = customers,
            TotalVendors = vendors,
            TotalItems = items,
            TotalInwardEntries = inwards,
            InwardThisMonth = monthInwards.TotalCount,
            InwardAmountThisMonth = monthAmount,
            TotalDcs = dcs,
            DcsThisMonth = monthDcs.TotalCount,
            DcAmountThisMonth = dcAmount,
            ItemsInStock = inStock,
            PendingDispatch = pending.TotalCount + pendingReceived.TotalCount,
            RecentInwards = recentInwards.Items.Select(x => new RecentActivityDto
            {
                Id = x.Id,
                Number = x.InwardNo,
                Date = x.InwardDate,
                Party = x.Customer?.Name ?? x.Vendor?.Name ?? string.Empty,
                TotalAmount = x.TotalAmount,
                Status = x.Status.ToString()
            }).ToList(),
            RecentDcs = recentDcs.Items.Select(x => new RecentActivityDto
            {
                Id = x.Id,
                Number = x.DcNo,
                Date = x.DcDate,
                Party = x.Customer?.Name ?? string.Empty,
                TotalAmount = x.TotalAmount,
                Status = x.Status.ToString()
            }).ToList()
        };
    }
}
