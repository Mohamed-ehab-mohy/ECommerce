namespace ECommerce.Infrastructure.ReadModels;

public sealed class DapperStockReadService(IReadModelStore readModelStore)
{
    private const string StockAvailabilitySql = @"
        SELECT s.sku AS Sku, w.code AS WarehouseCode,
               s.on_hand AS OnHand, s.allocated AS Allocated,
               (s.on_hand - s.allocated) AS Available
        FROM stock_items s
        JOIN warehouses w ON w.id = s.warehouse_id
        WHERE s.sku = @Sku AND w.is_active = true";

    public Task<IReadOnlyList<StockAvailabilityReadModel>> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        readModelStore.QueryAsync<StockAvailabilityReadModel>(StockAvailabilitySql, new { Sku = sku }, ct);
}
