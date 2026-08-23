namespace ECommerce.Infrastructure.ReadModels;

public sealed record StockAvailabilityReadModel(
    string Sku,
    string WarehouseCode,
    int OnHand,
    int Allocated,
    int Available);
