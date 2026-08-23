using ECommerce.Domain.Invoicing;

namespace ECommerce.UseCases.Invoicing.Ports;

public sealed record InvoiceListPage(
    IReadOnlyList<Invoice> Items,
    int TotalCount,
    int Page,
    int PageSize);

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Invoice?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<InvoiceListPage> ListAsync(
        InvoiceStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(Invoice invoice);
}
