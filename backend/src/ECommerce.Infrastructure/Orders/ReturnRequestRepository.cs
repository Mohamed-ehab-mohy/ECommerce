using ECommerce.Domain.Orders;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Orders.Ports;

namespace ECommerce.Infrastructure.Orders;

public sealed class ReturnRequestRepository(ECommerceDbContext dbContext) : IReturnRequestRepository
{
    public async Task<ReturnRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Set<ReturnRequest>()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ReturnRequest>> ListByOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        await dbContext.Set<ReturnRequest>()
            .Where(r => r.OrderId == orderId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReturnRequest>> ListPendingAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        await dbContext.Set<ReturnRequest>()
            .Where(r => r.Status == ReturnRequestStatus.Requested)
            .OrderBy(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<int> CountPendingAsync(CancellationToken cancellationToken) =>
        await dbContext.Set<ReturnRequest>()
            .CountAsync(r => r.Status == ReturnRequestStatus.Requested, cancellationToken);

    public void Add(ReturnRequest returnRequest) => dbContext.Set<ReturnRequest>().Add(returnRequest);
}
