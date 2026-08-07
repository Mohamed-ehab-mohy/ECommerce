using ECommerce.Domain.Payments;

namespace ECommerce.UseCases.Payments.Ports;

public interface IPaymentProviderFactory
{
    Task<IPaymentProvider> RouteAsync(string currency, string country, CancellationToken cancellationToken);

    Task<IPaymentProvider> GetAsync(string providerKey, CancellationToken cancellationToken);
}
