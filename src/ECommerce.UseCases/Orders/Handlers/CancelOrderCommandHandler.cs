using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Inventory.Ports;
using ECommerce.UseCases.Orders.Commands;
using ECommerce.UseCases.Orders.Ports;
using ECommerce.UseCases.Orders.Responses;
using ECommerce.UseCases.Payments.Ports;

namespace ECommerce.UseCases.Orders.Handlers;

public sealed class CancelOrderCommandHandler(
    IOrderRepository orders,
    IPaymentRepository payments,
    IStockAllocator stockAllocator,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<CancelOrderCommand> validator) : IRequestHandler<CancelOrderCommand, Result<OrderResponse>>
{
    public async Task<Result<OrderResponse>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<OrderResponse>();
        }

        if (!OrderNumber.TryParse(request.OrderNumber, out var orderNumber) || orderNumber is null)
        {
            return OrderErrors.OrderNotFound;
        }

        var order = await orders.GetByNumberWithDetailsAsync(orderNumber.Value, cancellationToken);
        if (order is null)
        {
            return OrderErrors.OrderNotFound;
        }

        if (request.RequesterCustomerId is { } requesterId
            && order.CustomerId != requesterId
            && !request.SupportAccess)
        {
            return OrderErrors.NotYourOrder;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var cancelResult = order.Cancel(
            request.Reason?.Trim() ?? "customer-request",
            request.SupportAccess ? "support" : "customer",
            request.RequesterCustomerId,
            null,
            utcNow);
        if (cancelResult.IsFailure)
        {
            return cancelResult.Error;
        }

        await stockAllocator.ReleaseAsync(
            order.Items
                .Select(item => new AllocationRequestItem(item.Sku, item.Quantity))
                .ToList(),
            "ORDER-CANCELLED",
            order.Id.ToString("N"),
            utcNow,
            cancellationToken);

        var payment = await payments.GetByOrderIdAsync(order.Id, cancellationToken);
        if (payment is not null && payment.Status == PaymentStatus.Captured)
        {
            var refundResult = payment.RequestRefund(request.Reason?.Trim() ?? "order-cancelled", utcNow);
            if (refundResult.IsFailure)
            {
                return refundResult.Error;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return OrderResponse.From(order);
    }
}
