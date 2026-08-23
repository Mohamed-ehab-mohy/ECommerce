using ECommerce.Domain.Orders;

namespace ECommerce.UseCases.Orders.Ports;

public interface IOrderNumberGenerator
{
    Task<string> GenerateAsync(DateTime utcNow, CancellationToken cancellationToken);
}
