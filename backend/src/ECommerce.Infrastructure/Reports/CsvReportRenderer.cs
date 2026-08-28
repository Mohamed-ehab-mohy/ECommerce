using System.Globalization;
using System.Text;
using ECommerce.UseCases.Reports.Responses;

namespace ECommerce.Infrastructure.Reports;

/// <summary>Renders report rows to RFC-4180-style CSV.</summary>
public static class CsvReportRenderer
{
    public static string RenderSales(IReadOnlyList<SalesPoint> points)
    {
        var builder = new StringBuilder();
        builder.AppendLine("PeriodStart,Orders,Revenue,Items");

        foreach (var point in points)
        {
            builder.Append(Escape(point.PeriodStart.ToString("O", CultureInfo.InvariantCulture)));
            builder.Append(',');
            builder.Append(point.Orders.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(point.Revenue.ToString("F2", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.AppendLine(point.Items.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    public static string RenderInventory(IReadOnlyList<InventoryLine> lines)
    {
        var builder = new StringBuilder();
        builder.AppendLine("WarehouseCode,WarehouseName,Sku,OnHand,Allocated,Available,LowStockThreshold,IsLow");

        foreach (var line in lines)
        {
            builder.Append(Escape(line.WarehouseCode));
            builder.Append(',');
            builder.Append(Escape(line.WarehouseName));
            builder.Append(',');
            builder.Append(Escape(line.Sku));
            builder.Append(',');
            builder.Append(line.OnHand.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(line.Allocated.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(line.Available.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(line.LowStockThreshold.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.AppendLine(line.IsLow ? "true" : "false");
        }

        return builder.ToString();
    }

    public static string RenderFinance(IReadOnlyList<FinanceLine> lines)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Currency,Collected,Refunded,AuthorizedOutstanding,Net");

        foreach (var line in lines)
        {
            builder.Append(Escape(line.Currency));
            builder.Append(',');
            builder.Append(line.Collected.ToString("F2", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(line.Refunded.ToString("F2", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(line.AuthorizedOutstanding.ToString("F2", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.AppendLine(line.Net.ToString("F2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    public static string RenderPromotions(IReadOnlyList<PromotionLine> lines)
    {
        var builder = new StringBuilder();
        builder.AppendLine("PromotionId,Name,State,OrdersApplied,TotalDiscount,CouponRedemptions");

        foreach (var line in lines)
        {
            builder.Append(line.PromotionId);
            builder.Append(',');
            builder.Append(Escape(line.Name));
            builder.Append(',');
            builder.Append(Escape(line.State));
            builder.Append(',');
            builder.Append(line.OrdersApplied.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(line.TotalDiscount.ToString("F2", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.AppendLine(line.CouponRedemptions.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    public static string RenderFulfillment(IReadOnlyList<FulfillmentWarehouseLine> warehouses)
    {
        var builder = new StringBuilder();
        builder.AppendLine("WarehouseCode,WarehouseName,TotalTasks,Shipped,Cancelled,AvgHoursToShip,OnTimeRate");

        foreach (var line in warehouses)
        {
            builder.Append(Escape(line.WarehouseCode));
            builder.Append(',');
            builder.Append(Escape(line.WarehouseName));
            builder.Append(',');
            builder.Append(line.TotalTasks.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(line.Shipped.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(line.Cancelled.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(line.AvgHoursToShip.ToString("F2", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.AppendLine(line.OnTimeRate.ToString("F1", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static string Escape(string value) =>
        value.IndexOfAny([',', '"', '\r', '\n']) < 0
            ? value
            : $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
