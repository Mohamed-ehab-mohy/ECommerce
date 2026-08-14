using ECommerce.Domain.Invoicing;

namespace ECommerce.UseCases.Invoicing.Responses;

public sealed record InvoiceLineResponse(
    Guid Id,
    string Sku,
    string Description,
    int Quantity,
    decimal UnitAmount,
    decimal TaxRate,
    decimal Amount);

public sealed record InvoiceResponse(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid OrderId,
    Guid? CustomerId,
    string Currency,
    InvoiceStatus Status,
    decimal TaxRate,
    decimal TaxAmount,
    decimal Total,
    decimal CreditedTotal,
    string? PdfUrl,
    DateTime IssuedAt,
    IReadOnlyList<InvoiceLineResponse> Lines)
{
    public static InvoiceResponse From(Invoice invoice) =>
        new(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.OrderId,
            invoice.CustomerId,
            invoice.Currency,
            invoice.Status,
            invoice.TaxRate,
            invoice.TaxAmount,
            invoice.Total,
            invoice.CreditedTotal,
            invoice.PdfUrl,
            invoice.IssuedAt,
            invoice.Lines
                .Select(line => new InvoiceLineResponse(
                    line.Id,
                    line.Sku,
                    line.Description,
                    line.Quantity,
                    line.UnitAmount,
                    line.TaxRate,
                    line.Amount))
                .ToList());
}
