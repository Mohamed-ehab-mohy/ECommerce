namespace ECommerce.Infrastructure.ReadModels;

public sealed class DapperProductReadService(IReadModelStore readModelStore)
{
    private const string ProductSummarySql = @"
        SELECT p.id AS Id, p.sku AS Sku, pt.name AS Name, p.slug AS Slug,
               pp.list_amount AS ListPrice,
               (p.status = 'active') AS IsActive
        FROM products p
        JOIN product_translations pt ON pt.product_id = p.id AND pt.locale = 'en'
        JOIN product_prices pp ON pp.product_id = p.id AND pp.currency = 'USD'
        WHERE p.sku = @Sku AND p.status = 'active'
        LIMIT 1";

    public Task<ProductSummaryReadModel?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        readModelStore.QueryFirstOrDefaultAsync<ProductSummaryReadModel>(ProductSummarySql, new { Sku = sku }, ct);
}
