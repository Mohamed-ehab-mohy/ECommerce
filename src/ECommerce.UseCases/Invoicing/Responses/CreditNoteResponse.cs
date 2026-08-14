using ECommerce.Domain.Invoicing;

namespace ECommerce.UseCases.Invoicing.Responses;

public sealed record CreditNoteResponse(
    Guid CreditNoteId,
    string CreditNoteNumber,
    Guid InvoiceId,
    Guid? RefundId,
    decimal Amount,
    string Reason,
    DateTime IssuedAt)
{
    public static CreditNoteResponse From(CreditNote creditNote) =>
        new(
            creditNote.Id,
            creditNote.CreditNoteNumber,
            creditNote.InvoiceId,
            creditNote.RefundId,
            creditNote.Amount,
            creditNote.Reason,
            creditNote.IssuedAt);
}
