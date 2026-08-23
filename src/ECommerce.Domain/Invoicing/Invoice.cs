using ECommerce.Domain.Common;
using ECommerce.Domain.Events;

namespace ECommerce.Domain.Invoicing;

/// <summary>
/// Financial invoice issued when a payment is captured (FR-09-001). Immutable copy of the order
/// pricing at issuance; credit notes only ever reduce the outstanding balance via
/// <see cref="ApplyCreditNote"/>.
/// </summary>
public sealed class Invoice : BaseEntity<Guid>
{
    private readonly List<InvoiceLine> _lines = [];

    private Invoice()
    {
        InvoiceNumber = string.Empty;
        Currency = string.Empty;
    }

    public string InvoiceNumber { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public string Currency { get; private set; }

    public decimal TaxAmount { get; private set; }

    public decimal TaxRate { get; private set; }

    public decimal Total { get; private set; }

    public InvoiceStatus Status { get; private set; }

    public string? PdfUrl { get; private set; }

    public DateTime IssuedAt { get; private set; }

    public IReadOnlyCollection<InvoiceLine> Lines => _lines;

    /// <summary>Cumulative value credited via credit notes against this invoice.</summary>
    public decimal CreditedTotal { get; private set; }

    public static Invoice Create(
        string invoiceNumber,
        Guid orderId,
        Guid? customerId,
        string currency,
        IReadOnlyList<InvoiceLine> lines,
        decimal taxAmount,
        decimal taxRate,
        decimal total,
        DateTime issuedAt)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            OrderId = orderId,
            CustomerId = customerId,
            Currency = currency,
            TaxAmount = taxAmount,
            TaxRate = taxRate,
            Total = total,
            Status = InvoiceStatus.Issued,
            IssuedAt = issuedAt,
            CreatedAt = issuedAt,
            UpdatedAt = issuedAt
        };

        foreach (var line in lines)
        {
            line.InvoiceId = invoice.Id;
            invoice._lines.Add(line);
        }

        invoice.AddDomainEvent(new InvoiceIssued(
            invoice.Id,
            invoice.InvoiceNumber,
            orderId,
            total,
            currency));

        return invoice;
    }

    /// <summary>Applies a credit note amount, advancing the status Issued → PartiallyRefunded → Refunded.</summary>
    public Result ApplyCreditNote(decimal amount, DateTime utcNow)
    {
        if (amount <= 0m)
        {
            return InvoiceErrors.InvalidCreditAmount;
        }

        if (Status is InvoiceStatus.Refunded or InvoiceStatus.Cancelled)
        {
            return InvoiceErrors.InvoiceNotCreditable;
        }

        var remaining = Total - CreditedTotal;
        if (amount > remaining)
        {
            return InvoiceErrors.CreditExceedsTotal;
        }

        CreditedTotal += amount;
        Status = amount >= remaining
            ? InvoiceStatus.Refunded
            : InvoiceStatus.PartiallyRefunded;
        UpdatedAt = utcNow;

        AddDomainEvent(new InvoiceCredited(Id, Guid.Empty, amount, Total - CreditedTotal));

        return Result.Success();
    }

    public void MarkPaid(DateTime utcNow)
    {
        if (Status == InvoiceStatus.Issued)
        {
            Status = InvoiceStatus.Paid;
            UpdatedAt = utcNow;
        }
    }

    public void AttachPdf(string pdfUrl, DateTime utcNow)
    {
        PdfUrl = pdfUrl;
        UpdatedAt = utcNow;
    }
}
