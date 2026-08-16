using ECommerce.Domain.Inventory;
using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Reports.Ports;
using ECommerce.UseCases.Reports.Responses;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Reports;

/// <summary>
/// Read-model query service for analytics reports (T-DAT-017). Aggregations run in the database on
/// covering indexes (orders by PlacedAt, ledger entries by OccurredAt, stock by warehouse/sku).
/// </summary>
public sealed class ReportingQueryService(ECommerceDbContext dbContext) : IReportingQueryService
{
    public async Task<IReadOnlyList<SalesPoint>> GetSalesSeriesAsync(
        SalesReportFilter filter,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Set<Order>().AsNoTracking()
            .Where(order => order.PlacedAt >= filter.From && order.PlacedAt <= filter.To)
            .Where(order => order.Status != OrderStatus.Cancelled)
            .Where(order => filter.Currency == null || order.Currency == filter.Currency)
            .Select(order => new { order.PlacedAt, order.GrandTotal, Quantity = order.Items.Sum(item => item.Quantity) })
            .ToListAsync(cancellationToken);

        // Bucketing is done in memory: reports are time-scoped (max 400 days) and Postgres date_trunc
        // is not exposed by the provider version in use. Weeks start on Monday like date_trunc('week').
        Func<DateTime, DateTime> bucket = filter.Granularity switch
        {
            "week" => StartOfWeek,
            "month" => StartOfMonth,
            _ => StartOfDay
        };

        var points = rows
            .GroupBy(row => bucket(row.PlacedAt!.Value))
            .Select(group => new SalesPoint(
                group.Key,
                group.Count(),
                group.Sum(row => row.GrandTotal),
                group.Sum(row => row.Quantity)))
            .OrderBy(point => point.PeriodStart)
            .ToList();

        return points;
    }

    public async Task<InventoryReportData> GetInventoryAsync(CancellationToken cancellationToken)
    {
        var warehouses = await dbContext.Set<Warehouse>().AsNoTracking()
            .Where(warehouse => !warehouse.IsDeleted)
            .ToDictionaryAsync(warehouse => warehouse.Id, cancellationToken);

        var stock = await dbContext.Set<StockItem>().AsNoTracking()
            .Where(item => !item.IsDeleted)
            .ToListAsync(cancellationToken);

        var lines = stock
            .Select(item =>
            {
                warehouses.TryGetValue(item.WarehouseId, out var warehouse);
                return new InventoryLine(
                    item.WarehouseId,
                    warehouse?.Code ?? "UNKNOWN",
                    warehouse?.Name ?? "Unknown warehouse",
                    item.Sku,
                    item.OnHand,
                    item.Allocated,
                    item.Available,
                    item.LowStockThreshold,
                    item.Available <= item.LowStockThreshold);
            })
            .ToList();

        return new InventoryReportData(lines, lines.Count(line => line.IsLow));
    }

    public async Task<IReadOnlyList<FinanceLine>> GetFinanceAsync(
        FinanceReportFilter filter,
        CancellationToken cancellationToken)
    {
        var currencyTotals = await dbContext.Set<PaymentLedgerEntry>().AsNoTracking()
            .Where(entry => entry.OccurredAt >= filter.From && entry.OccurredAt <= filter.To)
            .Where(entry => entry.EventType == "captured" || entry.EventType == "refunded")
            .Join(
                dbContext.Set<Payment>().AsNoTracking(),
                entry => entry.PaymentId,
                payment => payment.Id,
                (entry, payment) => new { entry.EventType, entry.Amount, payment.Currency })
            .GroupBy(row => row.Currency)
            .Select(group => new
            {
                Currency = group.Key,
                Collected = group.Sum(row => row.EventType == "captured" ? row.Amount : 0m),
                Refunded = group.Sum(row => row.EventType == "refunded" ? row.Amount : 0m)
            })
            .ToListAsync(cancellationToken);

        var outstanding = await dbContext.Set<Payment>().AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.Authorized)
            .GroupBy(payment => payment.Currency)
            .Select(group => new { Currency = group.Key, Amount = group.Sum(payment => payment.AuthorizedAmount) })
            .ToDictionaryAsync(row => row.Currency, cancellationToken);

        return currencyTotals
            .Select(row => new FinanceLine(
                row.Currency,
                row.Collected,
                row.Refunded,
                outstanding.TryGetValue(row.Currency, out var value) ? value.Amount : 0m,
                row.Collected - row.Refunded))
            .OrderBy(line => line.Currency)
            .ToList();
    }

    private static DateTime StartOfDay(DateTime value) => value.Date;

    private static DateTime StartOfWeek(DateTime value)
    {
        var daysSinceMonday = ((int)value.DayOfWeek + 6) % 7;
        return value.Date.AddDays(-daysSinceMonday);
    }

    private static DateTime StartOfMonth(DateTime value) => new(value.Year, value.Month, 1, 0, 0, 0, DateTimeKind.Utc);
}
