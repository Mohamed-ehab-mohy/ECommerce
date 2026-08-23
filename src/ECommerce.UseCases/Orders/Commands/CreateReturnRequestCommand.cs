using ECommerce.UseCases.Common;

namespace ECommerce.UseCases.Orders.Commands;

public sealed record CreateReturnRequestCommand(
    Guid OrderId, string Reason, bool Restock,
    IReadOnlyList<ReturnRequestItemDto> Items) : IRequest<Result<Guid>>;

public sealed record ReturnRequestItemDto(
    Guid OrderItemId, Guid ProductId, string Sku, int Quantity, decimal UnitPrice, string? Reason);
