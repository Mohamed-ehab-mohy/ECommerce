using ECommerce.Domain.Identity;

namespace ECommerce.UseCases.Identity.Ports;

public interface IUserRepository
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Customer?> GetByVerificationTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<Customer?> GetByResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Customer>> SearchAsync(string? email, int page, int pageSize, CancellationToken cancellationToken);

    Task<int> CountAsync(string? email, CancellationToken cancellationToken);

    void Add(Customer customer);

    void AddRole(UserRole userRole);
}
