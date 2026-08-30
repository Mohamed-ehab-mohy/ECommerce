using ECommerce.Domain.Orders;
using ECommerce.Infrastructure.Common;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Orders.Ports;
using Npgsql;

namespace ECommerce.Infrastructure.Orders;

public sealed class IdempotencyKeyRepository(ECommerceDbContext dbContext) : IIdempotencyKeyRepository
{
    public Task<IdempotencyKey?> GetByKeyAsync(string key, CancellationToken cancellationToken) =>
        dbContext.Set<IdempotencyKey>()
            .SingleOrDefaultAsync(idempotencyKey => idempotencyKey.Key == key, cancellationToken);

    public async Task<IdempotencyKey?> AddIfAbsentAsync(
        IdempotencyKey idempotencyKey,
        CancellationToken cancellationToken)
    {
        // The idempotency key must be stored scoped to the current tenant so that the
        // tenant-scoped reads (GetByKeyAsync) can find it again for replay. Raw SQL bypasses
        // TenantAwareSaveChangesInterceptor, so stamp tenant_id explicitly.
        var tenantId = dbContext.CurrentTenant
            ?? throw new InvalidOperationException("A tenant scope is required to persist an idempotency key.");

        var inserted = await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO idempotency_keys (id, tenant_id, key, checkout_id, order_id, created_at, updated_at, is_deleted)
            VALUES (@id, @tenant_id, @key, @checkout_id, @order_id, @created_at, @updated_at, FALSE)
            ON CONFLICT (key) DO NOTHING
            """,
            new object[]
            {
                new NpgsqlParameter("@id", idempotencyKey.Id),
                new NpgsqlParameter("@tenant_id", tenantId),
                new NpgsqlParameter("@key", idempotencyKey.Key),
                new NpgsqlParameter("@checkout_id", idempotencyKey.CheckoutId),
                new NpgsqlParameter("@order_id", idempotencyKey.OrderId),
                new NpgsqlParameter("@created_at", idempotencyKey.CreatedAt),
                new NpgsqlParameter("@updated_at", idempotencyKey.UpdatedAt),
            },
            cancellationToken);

        return inserted > 0 ? null : await GetByKeyAsync(idempotencyKey.Key, cancellationToken);
    }
}
