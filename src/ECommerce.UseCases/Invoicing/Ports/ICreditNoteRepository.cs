using ECommerce.Domain.Invoicing;

namespace ECommerce.UseCases.Invoicing.Ports;

public sealed record CreditNoteListPage(
    IReadOnlyList<CreditNote> Items,
    int TotalCount,
    int Page,
    int PageSize);

public interface ICreditNoteRepository
{
    Task<CreditNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CreditNote?> GetByRefundIdAsync(Guid refundId, CancellationToken cancellationToken);

    Task<CreditNoteListPage> ListByInvoiceAsync(
        Guid invoiceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(CreditNote creditNote);
}
