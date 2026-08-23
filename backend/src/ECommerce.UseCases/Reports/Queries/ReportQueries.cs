using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Reports.Responses;

namespace ECommerce.UseCases.Reports.Queries;

/// <summary>Sales time-series report (US-L-001, docs/08 §6.9).</summary>
public sealed record SalesReportQuery(
    DateTime? From,
    DateTime? To,
    string? Granularity,
    string? Currency)
    : IRequest<Result<SalesReportResponse>>, IRequirePermission
{
    public string Permission => Permissions.ReportsRead;
}

/// <summary>Inventory position report (US-L-002, docs/08 §6.9).</summary>
public sealed record InventoryReportQuery()
    : IRequest<Result<InventoryReportResponse>>, IRequirePermission
{
    public string Permission => Permissions.ReportsRead;
}

/// <summary>Finance report matching the payment ledger (US-L-006, docs/08 §6.9).</summary>
public sealed record FinanceReportQuery(DateTime? From, DateTime? To)
    : IRequest<Result<FinanceReportResponse>>, IRequirePermission
{
    public string Permission => Permissions.ReportsRead;
}

/// <summary>Promotion performance report (US-L-004, docs/08 §6.9).</summary>
public sealed record PromotionReportQuery(DateTime? From, DateTime? To)
    : IRequest<Result<PromotionReportResponse>>, IRequirePermission
{
    public string Permission => Permissions.ReportsRead;
}

/// <summary>Fulfillment SLA report (US-L-005, docs/08 §6.9).</summary>
public sealed record FulfillmentReportQuery(DateTime? From, DateTime? To)
    : IRequest<Result<FulfillmentReportResponse>>, IRequirePermission
{
    public string Permission => Permissions.ReportsRead;
}
