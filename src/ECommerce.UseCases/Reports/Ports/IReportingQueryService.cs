using ECommerce.UseCases.Reports.Responses;

namespace ECommerce.UseCases.Reports.Ports;

public sealed record SalesReportFilter(DateTime From, DateTime To, string Granularity, string? Currency);

public sealed record FinanceReportFilter(DateTime From, DateTime To);

public sealed record PromotionReportFilter(DateTime From, DateTime To);

public sealed record FulfillmentReportFilter(DateTime From, DateTime To);

/// <summary>
/// Read-model query service for analytics reports (T-DAT-017). Implementations query the read
/// models with covering indexes so reports stay within budget on large datasets.
/// </summary>
public interface IReportingQueryService
{
    Task<IReadOnlyList<SalesPoint>> GetSalesSeriesAsync(
        SalesReportFilter filter,
        CancellationToken cancellationToken);

    Task<InventoryReportData> GetInventoryAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<FinanceLine>> GetFinanceAsync(
        FinanceReportFilter filter,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PromotionLine>> GetPromotionsAsync(
        PromotionReportFilter filter,
        CancellationToken cancellationToken);

    Task<FulfillmentReportData> GetFulfillmentAsync(
        FulfillmentReportFilter filter,
        CancellationToken cancellationToken);
}

public sealed record InventoryReportData(
    IReadOnlyList<InventoryLine> Lines,
    int LowStockCount);

public sealed record FulfillmentReportData(
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
