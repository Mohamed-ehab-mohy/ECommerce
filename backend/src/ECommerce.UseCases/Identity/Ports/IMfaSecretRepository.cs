using ECommerce.Domain.Identity;

namespace ECommerce.UseCases.Identity.Ports;

public interface IMfaSecretRepository
{
    Task<MfaSecret?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken);
    void Add(MfaSecret mfaSecret);
}
