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

/// <summary>Finance totals for a single currency (matches the payment ledger, US-L-006).</summary>
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
