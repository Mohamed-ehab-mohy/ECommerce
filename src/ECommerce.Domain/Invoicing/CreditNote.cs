using ECommerce.Domain.Common;
using ECommerce.Domain.Events;

namespace ECommerce.Domain.Invoicing;

/// <summary>
/// Credit note referencing an invoice and (when available) the refund that triggered it (FR-09-002).
/// </summary>
public sealed class CreditNote : BaseEntity<Guid>
{
    private CreditNote()
    {
        CreditNoteNumber = string.Empty;
        Reason = string.Empty;
    }

    public string CreditNoteNumber { get; private set; }

    public Guid InvoiceId { get; private set; }

    public Guid? RefundId { get; private set; }

    public decimal Amount { get; private set; }

    public string Reason { get; private set; }

    public DateTime IssuedAt { get; private set; }

    public static CreditNote Create(
        string creditNoteNumber,
        Guid invoiceId,
        Guid? refundId,
        decimal amount,
        string reason,
        string currency,
        DateTime issuedAt)
    {
        var creditNote = new CreditNote
        {
            Id = Guid.NewGuid(),
            CreditNoteNumber = creditNoteNumber,
            InvoiceId = invoiceId,
            RefundId = refundId,
            Amount = amount,
            Reason = reason,
            IssuedAt = issuedAt,
            CreatedAt = issuedAt,
            UpdatedAt = issuedAt
        };

        creditNote.AddDomainEvent(new CreditNoteIssued(
            creditNote.Id,
            creditNote.CreditNoteNumber,
            invoiceId,
            refundId,
            amount,
            currency));

        return creditNote;
    }
}
