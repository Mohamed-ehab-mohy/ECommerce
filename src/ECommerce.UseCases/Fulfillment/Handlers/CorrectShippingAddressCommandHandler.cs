using ECommerce.Domain.Orders;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Commands;
using ECommerce.UseCases.Orders.Ports;

namespace ECommerce.UseCases.Fulfillment.Handlers;

public sealed class CorrectShippingAddressCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<CorrectShippingAddressCommand> validator) : IRequestHandler<CorrectShippingAddressCommand, Result>
{
    public async Task<Result> Handle(CorrectShippingAddressCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult();
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return OrderErrors.OrderNotFound;
        }

        var address = new AddressSnapshot(
            request.FullName.Trim(),
            string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            request.Street.Trim(),
            request.City.Trim(),
            string.IsNullOrWhiteSpace(request.Region) ? null : request.Region.Trim(),
            request.Country.Trim().ToUpperInvariant(),
            request.PostalCode.Trim());

        var correction = order.UpdateShippingAddress(address, "user", null, null, utcNow);
        if (correction.IsFailure)
        {
            return correction.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
