namespace ECommerce.UseCases.Invoicing.Ports;

public sealed record InvoiceDocumentLine(
    string Sku,
    string Description,
    int Quantity,
    decimal UnitAmount,
    decimal Amount);

public sealed record InvoiceDocument(
    string InvoiceNumber,
    string OrderNumber,
    DateTime IssuedAt,
    string Currency,
    string CustomerName,
    string CustomerEmail,
    string BillingAddress,
    IReadOnlyList<InvoiceDocumentLine> Lines,
    decimal Subtotal,
    decimal ItemDiscount,
    decimal ShippingTotal,
    decimal TaxRate,
    decimal TaxAmount,
    decimal Total);

public interface IInvoicePdfRenderer
{
    byte[] Render(InvoiceDocument document);
}
