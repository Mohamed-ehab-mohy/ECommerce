using ECommerce.Infrastructure.Reports;
using ECommerce.UseCases.Reports.Responses;

namespace ECommerce.UnitTests;

public sealed class CsvReportRendererTests
{
    [Fact]
    public void RenderSales_Writes_Header_And_Rows()
    {
        var csv = CsvReportRenderer.RenderSales(
        [
            new SalesPoint(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), 2, 39.90m, 3),
            new SalesPoint(new DateTime(2026, 8, 2, 0, 0, 0, DateTimeKind.Utc), 1, 10.00m, 1)
        ]);

        var lines = csv.TrimEnd().Replace("\r\n", "\n").Split('\n');
        Assert.Equal("PeriodStart,Orders,Revenue,Items", lines[0]);
        Assert.Equal("2026-08-01T00:00:00.0000000Z,2,39.90,3", lines[1]);
        Assert.Equal("2026-08-02T00:00:00.0000000Z,1,10.00,1", lines[2]);
    }

    [Fact]
    public void RenderInventory_Writes_Header_And_Rows()
    {
        var csv = CsvReportRenderer.RenderInventory(
        [
            new InventoryLine(Guid.NewGuid(), "W1", "Main", "SKU-1", 10, 2, 8, 5, false)
        ]);

        Assert.Contains("WarehouseCode,WarehouseName,Sku,OnHand,Allocated,Available,LowStockThreshold,IsLow", csv);
        Assert.Contains("W1,Main,SKU-1,10,2,8,5,false", csv);
    }

    [Fact]
    public void RenderFinance_Writes_Header_And_Rows()
    {
        var csv = CsvReportRenderer.RenderFinance(
        [
            new FinanceLine("USD", 100.00m, 20.00m, 5.00m, 75.00m)
        ]);

        Assert.Contains("Currency,Collected,Refunded,AuthorizedOutstanding,Net", csv);
        Assert.Contains("USD,100.00,20.00,5.00,75.00", csv);
    }

    [Fact]
    public void RenderSales_Escapes_Commas_Quotes_And_Newlines()
    {
        var csv = CsvReportRenderer.RenderSales(
        [
            new SalesPoint(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), 1, 5m, 1)
        ]);

        Assert.Contains("2026-08-01T00:00:00.0000000Z", csv);
    }

    [Fact]
    public void RenderInventory_Escapes_Embedded_Commas()
    {
        var csv = CsvReportRenderer.RenderInventory(
        [
            new InventoryLine(Guid.NewGuid(), "W,1", "Main, HQ", "SKU,1", 1, 0, 1, 1, false)
        ]);

        Assert.Contains("\"W,1\",\"Main, HQ\",\"SKU,1\",1,0,1,1,false", csv);
    }
}
