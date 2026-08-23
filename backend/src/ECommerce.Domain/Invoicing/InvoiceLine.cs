using ECommerce.Domain.Common;

namespace ECommerce.Domain.Invoicing;

public sealed class InvoiceLine
{
    private InvoiceLine()
    {
        Description = string.Empty;
        Sku = string.Empty;
    }

    public Guid InvoiceId { get; internal set; }

    public Guid Id { get; private set; }

    public string Sku { get; private set; }

    public string Description { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitAmount { get; private set; }

    public decimal TaxRate { get; private set; }

    public decimal Amount { get; private set; }

    public static InvoiceLine Create(
        Guid invoiceId,
        string sku,
        string description,
        int quantity,
        decimal unitAmount,
        decimal taxRate,
        decimal amount) =>
        new()
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Sku = sku,
            Description = description,
            Quantity = quantity,
            UnitAmount = unitAmount,
            TaxRate = taxRate,
            Amount = amount
        };
}
