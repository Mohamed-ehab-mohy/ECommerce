namespace ECommerce.Domain.Invoicing;

public enum InvoiceStatus
{
    Issued,
    Paid,
    PartiallyRefunded,
    Refunded,
    Cancelled
}
