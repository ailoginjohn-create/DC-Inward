using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InwardDC.App.Services;
using InwardDC.Application.Common;
using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Criteria;

namespace InwardDC.App.ViewModels;

public enum ReportKind
{
    Daily,
    Monthly,
    CustomerWise,
    ItemWise,
    InwardDetail,
    DispatchDetail
}

public partial class ReportsViewModel : ViewModelBase
{
    private readonly IReportService _reports;
    private readonly IExcelService _excel;
    private readonly IPdfService _pdf;
    private readonly IDialogService _dialogs;

    public ReportsViewModel(ICurrentUserService currentUser, IReportService reports,
        IExcelService excel, IPdfService pdf, IDialogService dialogs) : base(currentUser)
    {
        _reports = reports;
        _excel = excel;
        _pdf = pdf;
        _dialogs = dialogs;
        Title = "Reports";
        _reportKind = ReportKind.Daily;
        _fromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _toDate = DateTime.Today;
    }

    public ObservableCollection<DailySummaryDto> DailyRows { get; } = new();
    public ObservableCollection<MonthlySummaryDto> MonthlyRows { get; } = new();
    public ObservableCollection<CustomerWiseSummaryDto> CustomerRows { get; } = new();
    public ObservableCollection<ItemWiseSummaryDto> ItemRows { get; } = new();
    public ObservableCollection<ReportRowDto> DetailRows { get; } = new();

    public IReadOnlyList<ReportKind> ReportKinds => Enum.GetValues<ReportKind>();

    [ObservableProperty] private ReportKind _reportKind;
    [ObservableProperty] private DateTime _fromDate;
    [ObservableProperty] private DateTime _toDate;

    [RelayCommand]
    private async Task GenerateAsync()
    {
        await RunAsync(async () =>
        {
            var filter = new ReportPeriodFilter
            {
                FromDate = FromDate.Date,
                ToDate = ToDate.Date.AddDays(1).AddMilliseconds(-1)
            };

            switch (ReportKind)
            {
                case ReportKind.Daily:
                {
                    DailyRows.Clear();
                    foreach (var row in await _reports.GetDailySummaryAsync(filter))
                        DailyRows.Add(row);
                    SetSuccess($"Daily summary: {DailyRows.Count} day(s).");
                    break;
                }
                case ReportKind.Monthly:
                {
                    MonthlyRows.Clear();
                    foreach (var row in await _reports.GetMonthlySummaryAsync(filter))
                        MonthlyRows.Add(row);
                    SetSuccess($"Monthly summary: {MonthlyRows.Count} month(s).");
                    break;
                }
                case ReportKind.CustomerWise:
                {
                    CustomerRows.Clear();
                    foreach (var row in await _reports.GetCustomerWiseAsync(filter))
                        CustomerRows.Add(row);
                    SetSuccess($"Customer-wise summary: {CustomerRows.Count} customer(s).");
                    break;
                }
                case ReportKind.ItemWise:
                {
                    ItemRows.Clear();
                    foreach (var row in await _reports.GetItemWiseAsync(filter))
                        ItemRows.Add(row);
                    SetSuccess($"Item-wise summary: {ItemRows.Count} item(s).");
                    break;
                }
                case ReportKind.InwardDetail:
                {
                    DetailRows.Clear();
                    foreach (var row in await _reports.GetInwardDetailAsync(filter))
                        DetailRows.Add(row);
                    SetSuccess($"Inward detail: {DetailRows.Count} line(s).");
                    break;
                }
                case ReportKind.DispatchDetail:
                {
                    DetailRows.Clear();
                    foreach (var row in await _reports.GetDispatchDetailAsync(filter))
                        DetailRows.Add(row);
                    SetSuccess($"Dispatch detail: {DetailRows.Count} line(s).");
                    break;
                }
            }
        }, "Generating report...");
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        var rows = BuildRows();
        if (rows.Count == 0)
        {
            _dialogs.ShowWarning("Generate the report first.");
            return;
        }

        var path = _dialogs.PickSaveFile("Excel Workbook|*.xlsx", $"{ReportKind}.xlsx");
        if (path is null) return;

        await RunAsync(async () =>
        {
            var file = await _excel.ExportReportAsync(ReportKind.ToString(), rows, path);
            _dialogs.ShowInfo($"Exported to:\n{file}");
        }, "Exporting...");
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        var rows = BuildRows();
        if (rows.Count == 0)
        {
            _dialogs.ShowWarning("Generate the report first.");
            return;
        }

        var path = _dialogs.PickSaveFile("PDF Document|*.pdf", $"{ReportKind}.pdf");
        if (path is null) return;

        await RunAsync(async () =>
        {
            var file = await _pdf.GenerateReportPdfAsync(ReportKind.ToString(), rows, path);
            _dialogs.ShowInfo($"PDF saved to:\n{file}");
        }, "Generating PDF...");
    }

    private IReadOnlyList<ReportRowDto> BuildRows() => ReportKind switch
    {
        ReportKind.Daily => DailyRows.Select(r => new ReportRowDto
        {
            Date = r.Date,
            Number = r.Date.ToShortDateString(),
            Type = "Day",
            Quantity = r.InwardCount,
            Amount = r.InwardAmount
        }).ToList(),
        ReportKind.Monthly => MonthlyRows.Select(r => new ReportRowDto
        {
            Number = r.YearMonth,
            Type = "Month",
            Quantity = r.InwardCount,
            Amount = r.InwardAmount
        }).ToList(),
        ReportKind.CustomerWise => CustomerRows.Select(r => new ReportRowDto
        {
            Party = r.PartyName,
            Type = "Customer",
            Quantity = r.InwardUnits + r.DcUnits,
            Amount = r.InwardAmount + r.DcAmount
        }).ToList(),
        ReportKind.ItemWise => ItemRows.Select(r => new ReportRowDto
        {
            ItemName = r.ItemName,
            Type = "Item",
            Quantity = r.InwardUnits,
            Amount = r.InwardAmount
        }).ToList(),
        _ => DetailRows.ToList()
    };
}
