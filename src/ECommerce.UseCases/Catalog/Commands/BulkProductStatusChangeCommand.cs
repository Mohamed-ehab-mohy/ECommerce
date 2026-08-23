using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Commands;

public enum BulkProductStatusAction
{
    Activate,
    Deactivate
}

/// <summary>One item in a bulk product status change batch (US-M-008, BR-2308).</summary>
public sealed record BulkProductStatusItem(Guid ProductId, BulkProductStatusAction Action);

/// <summary>
/// Bulk product status change with a per-item error report; partial success is reported per item
/// (US-M-008, FR-13-008).
/// </summary>
public sealed record BulkProductStatusChangeCommand(IReadOnlyList<BulkProductStatusItem> Items)
    : IRequest<Result<BulkProductStatusChangeResponse>>, IRequirePermission
{
    public string Permission => Permissions.CatalogProductWrite;
}
