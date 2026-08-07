using CheckoutAggregate = ECommerce.Domain.Orders.Checkout;

namespace ECommerce.UseCases.Checkout.Ports;

public interface ICheckoutRepository
{
    Task<CheckoutAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(CheckoutAggregate checkout);
}
