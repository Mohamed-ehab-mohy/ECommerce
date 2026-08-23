using ECommerce.Domain.Payments;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Payments.Ports;

namespace ECommerce.Infrastructure.Payments;

public sealed class RefundRepository(ECommerceDbContext dbContext) : IRefundRepository
{
    public Task<Refund?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Refund>()
            .Include(refund => refund.Items)
            .SingleOrDefaultAsync(refund => refund.Id == id, cancellationToken);

    public Task<Refund?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        dbContext.Set<Refund>()
            .Include(refund => refund.Items)
            .SingleOrDefaultAsync(refund => refund.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<IReadOnlyList<Refund>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken) =>
        await dbContext.Set<Refund>()
            .Include(refund => refund.Items)
            .Where(refund => refund.PaymentId == paymentId)
            .OrderBy(refund => refund.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Refund>> ListFailedAsync(CancellationToken cancellationToken) =>
        await dbContext.Set<Refund>()
            .Include(refund => refund.Items)
            .Where(refund => refund.Status == RefundStatus.Failed)
            .OrderBy(refund => refund.UpdatedAt)
            .ToListAsync(cancellationToken);

    public void Add(Refund refund) => dbContext.Set<Refund>().Add(refund);
}
