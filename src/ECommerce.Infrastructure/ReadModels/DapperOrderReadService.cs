namespace ECommerce.Infrastructure.ReadModels;

public sealed class DapperOrderReadService(IReadModelStore readModelStore)
{
    private const string OrderHistorySql = @"
        SELECT id AS Id, order_number AS OrderNumber, status AS Status,
               grand_total AS GrandTotal, currency AS Currency,
               placed_at AS PlacedAt, created_at AS CreatedAt
        FROM orders
        WHERE customer_email = @CustomerEmail
        ORDER BY created_at DESC
        LIMIT @Limit OFFSET @Offset";

    public Task<IReadOnlyList<OrderHistoryReadModel>> GetHistoryAsync(
        string customerEmail, int limit = 20, int offset = 0, CancellationToken ct = default) =>
        readModelStore.QueryAsync<OrderHistoryReadModel>(
            OrderHistorySql, new { CustomerEmail = customerEmail, Limit = limit, Offset = offset }, ct);
}
