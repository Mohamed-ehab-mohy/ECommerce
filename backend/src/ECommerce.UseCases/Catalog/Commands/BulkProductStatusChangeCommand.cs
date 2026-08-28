using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Catalog.Responses;
using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Catalog.Commands;

public enum BulkProductStatusAction
{
    Activate,
    Deactivate
}

/// <summary>One item in a bulk product status change batch.</summary>
public sealed record BulkProductStatusItem(Guid ProductId, BulkProductStatusAction Action);

/// <summary>
/// Bulk product status change with a per-item error report; partial success is reported per item
///.
/// </summary>
public sealed record BulkProductStatusChangeCommand(IReadOnlyList<BulkProductStatusItem> Items)
    : IRequest<Result<BulkProductStatusChangeResponse>>, IRequirePermission
{
    public string Permission => Permissions.CatalogProductWrite;
}
