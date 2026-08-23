
namespace ECommerce.UseCases.Orders.Commands;

public sealed record GetReturnRequestQuery(Guid ReturnRequestId) : IRequest<Result<ReturnRequestResponse>>;

public sealed record ListReturnRequestsByOrderQuery(Guid OrderId) : IRequest<Result<IReadOnlyList<ReturnRequestResponse>>>;

public sealed record ReturnRequestResponse(
    Guid Id, Guid OrderId, string Reason, string Currency, decimal RefundAmount,
    bool Restock, string Status, string? AdminNotes, DateTime CreatedAt);
