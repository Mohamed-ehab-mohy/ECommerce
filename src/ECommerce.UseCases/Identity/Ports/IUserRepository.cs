using ECommerce.Domain.Identity;

namespace ECommerce.UseCases.Identity.Ports;

public interface IUserRepository
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Customer?> GetByVerificationTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<Customer?> GetByResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(Customer customer);
}
