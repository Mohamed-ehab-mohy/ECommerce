namespace ECommerce.UseCases.Reports.Responses;

/// <summary>One bucketed sales point (period start, orders, revenue, units).</summary>
public sealed record SalesPoint(DateTime PeriodStart, int Orders, decimal Revenue, int Items);

public sealed record SalesTotals(int Orders, decimal Revenue, int Items);

public sealed record SalesReportResponse(
    DateTime From,
    DateTime To,
    string Granularity,
    string? Currency,
    SalesTotals Totals,
    IReadOnlyList<SalesPoint> Series);

/// <summary>One stock line within a warehouse.</summary>
public sealed record InventoryLine(
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    string Sku,
    int OnHand,
    int Allocated,
    int Available,
    int LowStockThreshold,
    bool IsLow);

public sealed record WarehouseInventory(
    Guid WarehouseId,
    string Code,
    string Name,
    int OnHand,
    IReadOnlyList<InventoryLine> Lines);

public sealed record InventoryReportResponse(
    DateTime GeneratedAt,
    int TotalSkus,
    int LowStockCount,
    int TotalOnHand,
    IReadOnlyList<WarehouseInventory> Warehouses);

/// <summary>Finance totals for a single currency (matches the payment ledger).</summary>
public sealed record FinanceLine(
    string Currency,
    decimal Collected,
    decimal Refunded,
    decimal AuthorizedOutstanding,
    decimal Net);

public sealed record FinanceReportResponse(
    DateTime From,
    DateTime To,
    IReadOnlyList<FinanceLine> Lines);

/// <summary>Promotion performance line.</summary>
public sealed record PromotionLine(
    Guid PromotionId,
    string Name,
    string State,
    int OrdersApplied,
    decimal TotalDiscount,
    int CouponRedemptions);

public sealed record PromotionReportResponse(
    DateTime From,
    DateTime To,
    int TotalPromotions,
    int ActiveCount,
    decimal TotalDiscount,
    IReadOnlyList<PromotionLine> Promotions);

/// <summary>Fulfillment SLA warehouse line.</summary>
public sealed record FulfillmentWarehouseLine(
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    int TotalTasks,
    int Shipped,
    int Cancelled,
    double AvgHoursToShip,
    double OnTimeRate);

public sealed record FulfillmentReportResponse(
    DateTime From,
    DateTime To,
    int TotalTasks,
    int Queued,
    int Assigned,
    int Picking,
    int Packed,
    int Shipped,
    int Cancelled,
    double AvgHoursToShip,
    double OnTimeRate,
    IReadOnlyList<FulfillmentWarehouseLine> Warehouses);
