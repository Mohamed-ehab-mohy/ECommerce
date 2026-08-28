using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reports.Ports;
using ECommerce.UseCases.Reports.Queries;
using ECommerce.UseCases.Reports.Responses;

namespace ECommerce.UseCases.Reports.Handlers;

/// <summary>Executes the sales time-series report.</summary>
public sealed class SalesReportQueryHandler(
    IReportingQueryService reporting,
    IValidator<SalesReportQuery> validator,
    TimeProvider timeProvider) : IRequestHandler<SalesReportQuery, Result<SalesReportResponse>>
{
    public async Task<Result<SalesReportResponse>> Handle(
        SalesReportQuery request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<SalesReportResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var (from, to) = ReportRanges.Resolve(request.From, request.To, utcNow);
        var granularity = string.IsNullOrWhiteSpace(request.Granularity) ? "day" : request.Granularity.ToLowerInvariant();
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? null : request.Currency.Trim().ToUpperInvariant();

        var series = await reporting.GetSalesSeriesAsync(
            new SalesReportFilter(from, to, granularity, currency),
            cancellationToken);

        var totals = new SalesTotals(
            series.Sum(point => point.Orders),
            series.Sum(point => point.Revenue),
            series.Sum(point => point.Items));

        return Result<SalesReportResponse>.Success(
            new SalesReportResponse(from, to, granularity, currency, totals, series));
    }
}

/// <summary>Executes the inventory position report.</summary>
public sealed class InventoryReportQueryHandler(
    IReportingQueryService reporting,
    TimeProvider timeProvider) : IRequestHandler<InventoryReportQuery, Result<InventoryReportResponse>>
{
    public async Task<Result<InventoryReportResponse>> Handle(
        InventoryReportQuery request,
        CancellationToken cancellationToken)
    {
        var data = await reporting.GetInventoryAsync(cancellationToken);

        var warehouses = data.Lines
            .GroupBy(line => line.WarehouseId)
            .Select(group =>
            {
                var first = group.First();
                return new WarehouseInventory(
                    first.WarehouseId,
                    first.WarehouseCode,
                    first.WarehouseName,
                    group.Sum(line => line.OnHand),
                    group.OrderBy(line => line.Sku).ToList());
            })
            .OrderBy(warehouse => warehouse.Code)
            .ToList();

        return Result<InventoryReportResponse>.Success(new InventoryReportResponse(
            timeProvider.GetUtcNow().UtcDateTime,
            data.Lines.Select(line => line.Sku).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            data.LowStockCount,
            data.Lines.Sum(line => line.OnHand),
            warehouses));
    }
}

/// <summary>Executes the finance report matching the payment ledger.</summary>
public sealed class FinanceReportQueryHandler(
    IReportingQueryService reporting,
    IValidator<FinanceReportQuery> validator,
    TimeProvider timeProvider) : IRequestHandler<FinanceReportQuery, Result<FinanceReportResponse>>
{
    public async Task<Result<FinanceReportResponse>> Handle(
        FinanceReportQuery request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<FinanceReportResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var (from, to) = ReportRanges.Resolve(request.From, request.To, utcNow);

        var lines = await reporting.GetFinanceAsync(new FinanceReportFilter(from, to), cancellationToken);

        return Result<FinanceReportResponse>.Success(new FinanceReportResponse(from, to, lines));
    }
}

/// <summary>Executes the promotion performance report.</summary>
public sealed class PromotionReportQueryHandler(
    IReportingQueryService reporting,
    IValidator<PromotionReportQuery> validator,
    TimeProvider timeProvider) : IRequestHandler<PromotionReportQuery, Result<PromotionReportResponse>>
{
    public async Task<Result<PromotionReportResponse>> Handle(
        PromotionReportQuery request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PromotionReportResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var (from, to) = ReportRanges.Resolve(request.From, request.To, utcNow);

        var lines = await reporting.GetPromotionsAsync(new PromotionReportFilter(from, to), cancellationToken);

        return Result<PromotionReportResponse>.Success(new PromotionReportResponse(
            from,
            to,
            lines.Count,
            lines.Count(l => l.State == "Active"),
            lines.Sum(l => l.TotalDiscount),
            lines));
    }
}

/// <summary>Executes the fulfillment SLA report.</summary>
public sealed class FulfillmentReportQueryHandler(
    IReportingQueryService reporting,
    IValidator<FulfillmentReportQuery> validator,
    TimeProvider timeProvider) : IRequestHandler<FulfillmentReportQuery, Result<FulfillmentReportResponse>>
{
    public async Task<Result<FulfillmentReportResponse>> Handle(
        FulfillmentReportQuery request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<FulfillmentReportResponse>();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var (from, to) = ReportRanges.Resolve(request.From, request.To, utcNow);

        var data = await reporting.GetFulfillmentAsync(new FulfillmentReportFilter(from, to), cancellationToken);

        return Result<FulfillmentReportResponse>.Success(new FulfillmentReportResponse(
            from,
            to,
            data.TotalTasks,
            data.Queued,
            data.Assigned,
            data.Picking,
            data.Packed,
            data.Shipped,
            data.Cancelled,
            data.AvgHoursToShip,
            data.OnTimeRate,
            data.Warehouses));
    }
}
