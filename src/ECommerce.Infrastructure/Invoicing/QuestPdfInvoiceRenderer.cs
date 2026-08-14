using System.Globalization;
using ECommerce.UseCases.Invoicing.Ports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ECommerce.Infrastructure.Invoicing;

public sealed class QuestPdfInvoiceRenderer : IInvoicePdfRenderer
{
    static QuestPdfInvoiceRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
    }

    public byte[] Render(InvoiceDocument document)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(style => style.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Column(title =>
                        {
                            title.Item().Text("INVOICE").FontSize(22).Bold().FontColor(Colors.Blue.Darken3);
                            title.Item().Text($"No. {document.InvoiceNumber}").FontSize(10).FontColor(Colors.Grey.Darken2);
                        });
                        row.RelativeItem().AlignRight().Column(meta =>
                        {
                            meta.Item().Text($"Order: {document.OrderNumber}").FontSize(9).FontColor(Colors.Grey.Darken2);
                            meta.Item().Text($"Issued: {document.IssuedAt:yyyy-MM-dd HH:mm 'UTC'}").FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                    });
                    header.Item().PaddingTop(16).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(16).Column(content =>
                {
                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Column(billTo =>
                        {
                            billTo.Item().Text("Bill to").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                            billTo.Item().PaddingTop(4).Text(document.CustomerName).FontSize(10);
                            billTo.Item().Text(document.CustomerEmail).FontSize(9).FontColor(Colors.Grey.Darken2);
                            billTo.Item().Text(document.BillingAddress).FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                        row.RelativeItem().AlignRight().Column(billTo =>
                        {
                            billTo.Item().Text("Totals").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                            billTo.Item().PaddingTop(4).Text($"Subtotal: {Money(document.Subtotal)}");
                            billTo.Item().Text($"Discounts: -{Money(document.ItemDiscount)}");
                            billTo.Item().Text($"Shipping: {Money(document.ShippingTotal)}");
                            billTo.Item().Text($"Tax ({(document.TaxRate * 100):0.###}%): {Money(document.TaxAmount)}");
                            billTo.Item().PaddingTop(4).Text($"Total: {Money(document.Total)}").Bold();
                        });
                    });

                    content.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(70);
                            columns.RelativeColumn();
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(90);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Lighten5).Padding(6).Text("SKU").FontSize(9).Bold();
                            header.Cell().Background(Colors.Blue.Lighten5).Padding(6).Text("Description").FontSize(9).Bold();
                            header.Cell().Background(Colors.Blue.Lighten5).Padding(6).Text("Qty").FontSize(9).Bold();
                            header.Cell().Background(Colors.Blue.Lighten5).Padding(6).Text("Unit").FontSize(9).Bold();
                            header.Cell().Background(Colors.Blue.Lighten5).Padding(6).Text("Amount").FontSize(9).Bold();
                        });

                        foreach (var line in document.Lines)
                        {
                            table.Cell().Padding(6).Text(line.Sku).FontSize(9);
                            table.Cell().Padding(6).Text(line.Description).FontSize(9);
                            table.Cell().Padding(6).Text(line.Quantity.ToString(CultureInfo.InvariantCulture)).FontSize(9);
                            table.Cell().Padding(6).Text(Money(line.UnitAmount)).FontSize(9);
                            table.Cell().Padding(6).Text(Money(line.Amount)).FontSize(9);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(
                    $"Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm 'UTC'} — {document.InvoiceNumber} · {document.OrderNumber}")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();

        string Money(decimal amount) =>
            $"{document.Currency} {amount.ToString("N2", CultureInfo.InvariantCulture)}";
    }
}
