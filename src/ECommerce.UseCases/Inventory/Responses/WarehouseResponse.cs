using ECommerce.Domain.Inventory;

namespace ECommerce.UseCases.Inventory.Responses;

public sealed record WarehouseResponse(
    Guid Id,
    string Code,
    string Name,
    string Address,
    string Timezone,
    WarehouseStatus Status);

public sealed record PagedWarehousesResponse(
    IReadOnlyList<WarehouseResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
