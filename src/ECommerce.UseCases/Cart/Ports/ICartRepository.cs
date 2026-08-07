using CartAggregate = ECommerce.Domain.Cart.Cart;

namespace ECommerce.UseCases.Cart.Ports;

public interface ICartRepository
{
    Task<CartAggregate?> ByOwnerKeyAsync(string ownerKey, CancellationToken cancellationToken);

    Task<CartAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task SaveAsync(CartAggregate cart, CancellationToken cancellationToken);
}
