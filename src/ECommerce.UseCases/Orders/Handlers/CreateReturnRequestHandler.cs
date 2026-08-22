using ECommerce.Domain.Orders;
using ECommerce.Shared.Errors;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Orders.Commands;
using ECommerce.UseCases.Orders.Ports;

namespace ECommerce.UseCases.Orders.Handlers;

public sealed class CreateReturnRequestHandler(
    IReturnRequestRepository returnRequests, IOrderRepository orders,
    IUnitOfWork unitOfWork, TimeProvider timeProvider) : IRequestHandler<CreateReturnRequestCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateReturnRequestCommand request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByNumberAsync(request.OrderId.ToString(), cancellationToken);

        if (order is null)
            return Result<Guid>.Failure(new Error("ReturnRequest.OrderNotFound", "Order not found"));

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var items = request.Items.Select(i => ReturnRequestItem.Create(
            i.OrderItemId, i.ProductId, i.Sku, i.Quantity, i.UnitPrice, i.Reason)).ToList();

        var refundAmount = items.Sum(i => i.UnitPrice * i.Quantity);
        var returnRequest = ReturnRequest.Create(
            order.Id, order.CustomerId ?? Guid.Empty, request.Reason,
            order.Currency, refundAmount, request.Restock, items, utcNow);

        returnRequests.Add(returnRequest);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(returnRequest.Id);
    }
}
