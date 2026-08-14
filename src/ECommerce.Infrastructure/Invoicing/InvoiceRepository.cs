using ECommerce.Domain.Invoicing;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Invoicing.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Invoicing;

public sealed class InvoiceRepository(ECommerceDbContext dbContext) : IInvoiceRepository
{
    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Invoice>()
            .Include(invoice => invoice.Lines)
            .SingleOrDefaultAsync(invoice => invoice.Id == id, cancellationToken);

    public Task<Invoice?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Set<Invoice>()
            .Include(invoice => invoice.Lines)
            .SingleOrDefaultAsync(invoice => invoice.OrderId == orderId, cancellationToken);

    public async Task<InvoiceListPage> ListAsync(
        InvoiceStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Set<Invoice>().AsNoTracking();

        if (status is not null)
        {
            query = query.Where(invoice => invoice.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(invoice => invoice.IssuedAt)
            .ThenBy(invoice => invoice.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new InvoiceListPage(items, totalCount, page, pageSize);
    }

    public void Add(Invoice invoice) => dbContext.Set<Invoice>().Add(invoice);
}
