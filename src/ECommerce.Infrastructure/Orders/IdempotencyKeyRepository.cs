using ECommerce.Domain.Orders;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Orders.Ports;
using Microsoft.EntityFrameworkCore;
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
        var inserted = await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO idempotency_keys (id, key, checkout_id, order_id, created_at, updated_at, is_deleted)
            VALUES (@id, @key, @checkout_id, @order_id, @created_at, @updated_at, FALSE)
            ON CONFLICT (key) DO NOTHING
            """,
            new NpgsqlParameter("@id", idempotencyKey.Id),
            new NpgsqlParameter("@key", idempotencyKey.Key),
            new NpgsqlParameter("@checkout_id", idempotencyKey.CheckoutId),
            new NpgsqlParameter("@order_id", idempotencyKey.OrderId),
            new NpgsqlParameter("@created_at", idempotencyKey.CreatedAt),
            new NpgsqlParameter("@updated_at", idempotencyKey.UpdatedAt),
            cancellationToken);

        return inserted > 0 ? null : await GetByKeyAsync(idempotencyKey.Key, cancellationToken);
    }
}
