using ECommerce.Domain.Invoicing;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Invoicing.Ports;

namespace ECommerce.Infrastructure.Invoicing;

public sealed class CreditNoteRepository(ECommerceDbContext dbContext) : ICreditNoteRepository
{
    public Task<CreditNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<CreditNote>()
            .SingleOrDefaultAsync(creditNote => creditNote.Id == id, cancellationToken);

    public Task<CreditNote?> GetByRefundIdAsync(Guid refundId, CancellationToken cancellationToken) =>
        dbContext.Set<CreditNote>()
            .SingleOrDefaultAsync(creditNote => creditNote.RefundId == refundId, cancellationToken);

    public async Task<CreditNoteListPage> ListByInvoiceAsync(
        Guid invoiceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Set<CreditNote>().AsNoTracking()
            .Where(creditNote => creditNote.InvoiceId == invoiceId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(creditNote => creditNote.IssuedAt)
            .ThenBy(creditNote => creditNote.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new CreditNoteListPage(items, totalCount, page, pageSize);
    }

    public void Add(CreditNote creditNote) => dbContext.Set<CreditNote>().Add(creditNote);
}
