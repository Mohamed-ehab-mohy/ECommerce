using ECommerce.Domain.Payments;
using ECommerce.Infrastructure.Common;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Payments.Ports;

namespace ECommerce.Infrastructure.Payments;

public sealed class PaymentRepository(ECommerceDbContext dbContext) : IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Payment>()
            .Include(payment => payment.Attempts)
            .Include(payment => payment.Ledger)
            .SingleOrDefaultAsync(payment => payment.Id == id, cancellationToken);

    public Task<Payment?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = dbContext.CurrentTenant;
        return tenantId is null
            ? Task.FromResult<Payment?>(null)
            : GetByIdForUpdateAsyncCore(id, tenantId.Value, cancellationToken);
    }

    private async Task<Payment?> GetByIdForUpdateAsyncCore(Guid id, Guid tenantId, CancellationToken cancellationToken) =>
        await dbContext.Set<Payment>()
            .FromSqlInterpolated($"""SELECT * FROM "payments" WHERE "id" = {id} AND "tenant_id" = {tenantId} FOR UPDATE""")
            .Include(payment => payment.Attempts)
            .Include(payment => payment.Ledger)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Set<Payment>()
            .Include(payment => payment.Attempts)
            .Include(payment => payment.Ledger)
            .SingleOrDefaultAsync(payment => payment.OrderId == orderId, cancellationToken);

    public async Task<IReadOnlyList<Payment>> GetUnreconciledAsync(CancellationToken cancellationToken) =>
        await dbContext.Set<Payment>()
            .Where(payment => payment.ProviderReference != null)
            .Where(payment => !dbContext.Set<PaymentReconciliationRecord>()
                .Any(record => record.PaymentId == payment.Id))
            .OrderBy(payment => payment.UpdatedAt)
            .ThenBy(payment => payment.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentReconciliationRecord>> GetReconciliationRecordsAsync(
        ReconciliationStatus? status,
        CancellationToken cancellationToken) =>
        await dbContext.Set<PaymentReconciliationRecord>()
            .Where(record => status == null || record.Status == status)
            .OrderBy(record => record.CheckedAtUtc)
            .ThenBy(record => record.Id)
            .ToListAsync(cancellationToken);

    public Task<Payment?> GetByProviderTokenAsync(string providerToken, CancellationToken cancellationToken) =>
        dbContext.Set<Payment>()
            .Include(payment => payment.Attempts)
            .Include(payment => payment.Ledger)
            .SingleOrDefaultAsync(payment => payment.ProviderToken == providerToken, cancellationToken);

    public void Add(Payment payment) => dbContext.Set<Payment>().Add(payment);

    public void AddReconciliationRecord(PaymentReconciliationRecord record) =>
        dbContext.Set<PaymentReconciliationRecord>().Add(record);
}
