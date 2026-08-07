using InwardDC.Application.DTOs;
using InwardDC.Application.Interfaces;
using InwardDC.Domain.Entities;
using InwardDC.Domain.Enums;
using InwardDC.Domain.Exceptions;
using InwardDC.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InwardDC.Infrastructure.Services;

/// <summary>
/// PDF generation built on QuestPDF (Community license, fully offline / zero cost).
/// Produces professional DC and Inward documents plus printable reports.
/// </summary>
public class PdfService : IPdfService
{
    private readonly IUnitOfWork _uow;
    private readonly ISettingsService _settings;
    private readonly ILogger<PdfService> _logger;

    public PdfService(IUnitOfWork uow, ISettingsService settings, ILogger<PdfService> logger)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _uow = uow;
        _settings = settings;
        _logger = logger;
    }

    public async Task<string> GenerateDcPdfAsync(Guid dispatchId, string outputPath, CancellationToken ct = default)
    {
        var dc = await _uow.DCs.GetByIdAsync(dispatchId, ct)
            ?? throw new NotFoundException("Dispatch Challan not found.");
        var company = await _settings.GetCompanySettingsAsync(ct);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? ".");

        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Segoe UI"));

                page.Header().Element(c => HeaderBlock(c, company, "DISPATCH CHALLAN"));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(8);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem(1).Element(c => PartyBlock(c, "Dispatch To (Customer)", dc.Customer?.Name ?? "", CustomerAddress(dc.Customer)));
                        row.RelativeItem(1).Element(c =>
                        {
                            c.Column(inner =>
                            {
                                inner.Spacing(2);
                                InfoRow(inner, "DC No", dc.DcNo);
                                InfoRow(inner, "DC Date", dc.DcDate.ToShortDateString());
                                InfoRow(inner, "Source Inward", dc.SourceInwardEntry?.InwardNo ?? "-");
                                InfoRow(inner, "Reference Challan", dc.ReferenceChallanNo);
                                InfoRow(inner, "Transport", dc.TransportDetails);
                                if (!string.IsNullOrWhiteSpace(dc.Remarks))
                                    InfoRow(inner, "Remarks", dc.Remarks);
                            });
                        });
                    });

                    column.Item().Text(t =>
                    {
                        t.Span("Items").FontSize(12).SemiBold();
                    });

                    column.Item().Element(c => ItemsTable(c, dc.Items.Select(i => new PdfLine
                    {
                        Name = i.ItemName,
                        Make = i.ItemMake,
                        Model = i.ItemModel,
                        Serial = string.Join("\n", i.Serials.Where(s => !s.IsDeleted).Select(s => s.SerialNo)),
                        Quantity = i.Quantity,
                        Unit = i.Unit,
                        Rate = i.Rate,
                        Amount = i.Amount
                    }).ToList(), dc.TotalQuantity, dc.TotalAmount));

                    column.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem(1).Element(c => SignatureBlock(c, "Prepared By"));
                        row.RelativeItem(1).Element(c => SignatureBlock(c, "Received By"));
                    });

                    if (!string.IsNullOrWhiteSpace(company.FooterNote))
                    {
                        column.Item().PaddingTop(20).Text(company.FooterNote).FontSize(8).FontColor(Colors.Grey.Darken2);
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span($"{company.CompanyName}  |  Generated on {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(outputPath);

        return outputPath;
    }

    public async Task<string> GenerateInwardPdfAsync(Guid inwardId, string outputPath, CancellationToken ct = default)
    {
        var inward = await _uow.Inwards.GetByIdAsync(inwardId, ct)
            ?? throw new NotFoundException("Inward entry not found.");
        var company = await _settings.GetCompanySettingsAsync(ct);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? ".");

        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Segoe UI"));

                page.Header().Element(c => HeaderBlock(c, company, "INWARD ENTRY"));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(8);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem(1).Element(c => PartyBlock(c, "Received From",
                            inward.Customer?.Name ?? inward.Vendor?.Name ?? "", CustomerAddress(inward.Customer)));
                        row.RelativeItem(1).Element(c =>
                        {
                            c.Column(inner =>
                            {
                                inner.Spacing(2);
                                InfoRow(inner, "Inward No", inward.InwardNo);
                                InfoRow(inner, "Inward Date", inward.InwardDate.ToShortDateString());
                                InfoRow(inner, "Type", inward.InwardType.ToString());
                                InfoRow(inner, "Invoice No", inward.ReferenceInvoiceNo);
                                InfoRow(inner, "Invoice Date", inward.ReferenceInvoiceDate?.ToShortDateString() ?? "-");
                                InfoRow(inner, "Challan No", inward.ChallanNo);
                                InfoRow(inner, "Transport", inward.TransportDetails);
                                if (!string.IsNullOrWhiteSpace(inward.Remarks))
                                    InfoRow(inner, "Remarks", inward.Remarks);
                            });
                        });
                    });

                    column.Item().Text("Items").FontSize(12).SemiBold();

                    column.Item().Element(c => ItemsTable(c, inward.Items.Select(i => new PdfLine
                    {
                        Name = i.ItemName,
                        Make = i.ItemMake,
                        Model = i.ItemModel,
                        Serial = string.Join("\n", i.Serials.Where(s => !s.IsDeleted).Select(s => s.SerialNo)),
                        Quantity = i.Quantity,
                        Unit = i.Unit,
                        Rate = i.Rate,
                        Amount = i.Amount
                    }).ToList(), inward.TotalQuantity, inward.TotalAmount));

                    column.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem(1).Element(c => SignatureBlock(c, "Received By"));
                        row.RelativeItem(1).Element(c => SignatureBlock(c, "Authorised Signatory"));
                    });

                    if (!string.IsNullOrWhiteSpace(company.FooterNote))
                    {
                        column.Item().PaddingTop(20).Text(company.FooterNote).FontSize(8).FontColor(Colors.Grey.Darken2);
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span($"{company.CompanyName}  |  Generated on {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(outputPath);

        return outputPath;
    }

    public Task<string> GenerateReportPdfAsync(string title, IReadOnlyList<ReportRowDto> rows, string outputPath, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? ".");
        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(9).FontFamily("Segoe UI"));

                page.Header().AlignCenter().Text(title).FontSize(16).SemiBold();

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(65);
                        c.RelativeColumn(3);
                        c.RelativeColumn(3.5f);
                        c.RelativeColumn(3);
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        foreach (var hdr in new[] { "Date", "Number", "Type", "Party", "Item", "Qty", "Rate", "Amount" })
                            h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text(hdr).FontColor(Colors.White).SemiBold();
                    });

                    foreach (var row in rows)
                    {
                        table.Cell().Padding(3).Text(row.Date.ToShortDateString());
                        table.Cell().Padding(3).Text(row.Number);
                        table.Cell().Padding(3).Text(row.Type);
                        table.Cell().Padding(3).Text(row.Party);
                        table.Cell().Padding(3).Text(row.ItemName);
                        table.Cell().Padding(3).Text(row.Quantity.ToString());
                        table.Cell().Padding(3).Text(row.Rate.ToString("N2"));
                        table.Cell().Padding(3).Text(row.Amount.ToString("N2"));
                    }

                    table.Cell().ColumnSpan(7).Padding(4).AlignRight().Text("TOTAL").SemiBold();
                    table.Cell().Padding(4).Text(rows.Sum(r => r.Amount).ToString("N2")).SemiBold();
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span($"Generated on {DateTime.Now:dd MMM yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(outputPath);

        return Task.FromResult(outputPath);
    }

    private static void HeaderBlock(IContainer c, CompanySettingsDto company, string title)
    {
        c.Row(row =>
        {
            row.RelativeItem(2.2f).Element(b =>
            {
                b.Column(col =>
                {
                    col.Item().Text(company.CompanyName).FontSize(16).SemiBold();
                    col.Item().Text(CompanyAddress(company)).FontSize(8).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrWhiteSpace(company.CompanyGSTIN))
                        col.Item().Text($"GSTIN: {company.CompanyGSTIN}").FontSize(8).FontColor(Colors.Grey.Darken2);
                });
            });
            row.ConstantItem(3);
            row.RelativeItem(1).Element(b =>
            {
                b.Border(1).BorderColor(Colors.Grey.Medium).Background(Colors.Grey.Lighten4).Padding(8)
                    .AlignCenter().Text(title).FontSize(14).SemiBold();
            });
        });
    }

    private static void PartyBlock(IContainer c, string label, string name, string address)
    {
        c.Border(0.75f).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(col =>
        {
            col.Spacing(2);
            col.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken2).SemiBold();
            col.Item().Text(name).FontSize(12).SemiBold();
            if (!string.IsNullOrWhiteSpace(address))
                col.Item().Text(address).FontSize(9);
        });
    }

    private static void InfoRow(QuestPDF.Fluent.ColumnDescriptor c, string label, string value)
    {
        c.Item().Row(row =>
        {
            row.ConstantItem(110).Text(label).FontSize(9).SemiBold().FontColor(Colors.Grey.Darken2);
            row.RelativeItem().Text(value).FontSize(9);
        });
    }

    private static void ItemsTable(IContainer c, List<PdfLine> lines, decimal totalQty, decimal totalAmount)
    {
        c.Table(table =>
        {
            table.ColumnsDefinition(col =>
            {
                col.ConstantColumn(26);
                col.RelativeColumn(3);
                col.RelativeColumn(2);
                col.RelativeColumn(2.5f);
                col.ConstantColumn(48);
                col.ConstantColumn(48);
                col.ConstantColumn(70);
            });

            table.Header(h =>
            {
                var headers = new[] { "#", "Item", "Make / Model", "Serial Number", "Qty", "Unit", "Amount" };
                foreach (var hdr in headers)
                    h.Cell().Background(Colors.Blue.Darken3).Padding(4).Text(hdr).FontColor(Colors.White).SemiBold().FontSize(9);
            });

            int sr = 1;
            foreach (var line in lines)
            {
                table.Cell().Padding(3).Text(sr++.ToString()).FontSize(9);
                table.Cell().Padding(3).Text(line.Name).FontSize(9);
                table.Cell().Padding(3).Text($"{line.Make} {line.Model}".Trim()).FontSize(9);
                table.Cell().Padding(3).Text(line.Serial).FontSize(9);
                table.Cell().Padding(3).Text(line.Quantity.ToString()).FontSize(9);
                table.Cell().Padding(3).Text(line.Unit).FontSize(9);
                table.Cell().Padding(3).Text(line.Amount.ToString("N2")).FontSize(9);
            }

            table.Cell().ColumnSpan(3).Padding(4).Text("TOTAL").SemiBold().FontSize(9);
            table.Cell().ColumnSpan(2).Padding(4).Text($"{totalQty}").SemiBold().FontSize(9);
            table.Cell().Padding(4).Text("").FontSize(9);
            table.Cell().Padding(4).Text(totalAmount.ToString("N2")).SemiBold().FontSize(9);
        });
    }

    private static void SignatureBlock(IContainer c, string label)
    {
        c.Column(col =>
        {
            col.Spacing(40);
            col.Item().Text("_________________________").FontSize(9);
            col.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken2);
        });
    }

    private static string CompanyAddress(CompanySettingsDto c)
    {
        var parts = new[] { c.CompanyAddressLine1, c.CompanyAddressLine2, c.CompanyCity, c.CompanyState, c.CompanyPincode }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        var addr = string.Join(", ", parts);
        var contact = string.Join(" | ", new[] { c.CompanyPhone, c.CompanyEmail }.Where(p => !string.IsNullOrWhiteSpace(p)));
        return string.Join(Environment.NewLine, new[] { addr, contact }.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string CustomerAddress(Customer? customer)
    {
        if (customer is null) return string.Empty;
        var parts = new[] { customer.AddressLine1, customer.AddressLine2, customer.City, customer.State, customer.Pincode }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(", ", parts);
    }

    private sealed class PdfLine
    {
        public string Name { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Serial { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    }
}
