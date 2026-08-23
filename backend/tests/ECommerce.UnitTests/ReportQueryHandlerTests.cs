using ECommerce.UseCases.Reports.Handlers;
using ECommerce.UseCases.Reports.Ports;
using ECommerce.UseCases.Reports.Queries;
using ECommerce.UseCases.Reports.Responses;

namespace ECommerce.UnitTests;

public sealed class ReportQueryHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeReportingQueryService _reporting = new();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public async Task Sales_Resolves_Default_Range_And_Computes_Totals()
    {
        _reporting.Sales =
        [
            new SalesPoint(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), 2, 39.90m, 3),
            new SalesPoint(new DateTime(2026, 8, 2, 0, 0, 0, DateTimeKind.Utc), 1, 10.00m, 1)
        ];
        var handler = new SalesReportQueryHandler(
            _reporting,
            new SalesReportQueryValidator(),
            new FixedTimeProvider(UtcNow));

        var result = await handler.Handle(
            new SalesReportQuery(null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(UtcNow.AddDays(-30), result.Value.From);
        Assert.Equal(UtcNow, result.Value.To);
        Assert.Equal("day", result.Value.Granularity);
        Assert.Equal(3, result.Value.Totals.Orders);
        Assert.Equal(49.90m, result.Value.Totals.Revenue);
        Assert.Equal(4, result.Value.Totals.Items);
        Assert.Equal(2, result.Value.Series.Count);
    }

    [Fact]
    public async Task Sales_Normalizes_Granularity_And_Currency()
    {
        _reporting.Sales = [];
        var handler = new SalesReportQueryHandler(
            _reporting,
            new SalesReportQueryValidator(),
            new FixedTimeProvider(UtcNow));

        var result = await handler.Handle(
            new SalesReportQuery(null, null, "week", "usd"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal("week", result.Value.Granularity);
        Assert.Equal("USD", result.Value.Currency);
    }

    [Fact]
    public async Task Sales_Rejects_Range_Over_400_Days()
    {
        var handler = new SalesReportQueryHandler(
            _reporting,
            new SalesReportQueryValidator(),
            new FixedTimeProvider(UtcNow));

        var result = await handler.Handle(
            new SalesReportQuery(UtcNow.AddDays(-401), UtcNow, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Inventory_Groups_By_Warehouse_And_Counts_Skus()
    {
        var warehouseId = Guid.NewGuid();
        _reporting.Inventory = new InventoryReportData(
            [
                new InventoryLine(warehouseId, "W1", "Main", "SKU-1", 10, 2, 8, 5, false),
                new InventoryLine(warehouseId, "W1", "Main", "SKU-2", 3, 0, 3, 5, true),
                new InventoryLine(Guid.NewGuid(), "W2", "Second", "SKU-3", 20, 5, 15, 10, true)
            ],
            LowStockCount: 2);
        var handler = new InventoryReportQueryHandler(_reporting, new FixedTimeProvider(UtcNow));

        var result = await handler.Handle(new InventoryReportQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(3, result.Value.TotalSkus);
        Assert.Equal(2, result.Value.LowStockCount);
        Assert.Equal(33, result.Value.TotalOnHand);
        Assert.Equal(2, result.Value.Warehouses.Count);
        var main = result.Value.Warehouses[0];
        Assert.Equal("W1", main.Code);
        Assert.Equal(13, main.OnHand);
        Assert.Equal(2, main.Lines.Count);
    }

    [Fact]
    public async Task Finance_Resolves_Range_And_Returns_Lines()
    {
        _reporting.Finance =
        [
            new FinanceLine("USD", 100.00m, 20.00m, 5.00m, 75.00m)
        ];
        var handler = new FinanceReportQueryHandler(
            _reporting,
            new FinanceReportQueryValidator(),
            new FixedTimeProvider(UtcNow));

        var result = await handler.Handle(
            new FinanceReportQuery(null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(UtcNow.AddDays(-30), result.Value.From);
        Assert.Equal(UtcNow, result.Value.To);
        var line = Assert.Single(result.Value.Lines);
        Assert.Equal("USD", line.Currency);
        Assert.Equal(75.00m, line.Net);
    }
}
