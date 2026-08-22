using ECommerce.Domain.Identity;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.Infrastructure.Identity;

public sealed class UserRepository(ECommerceDbContext dbContext) : IUserRepository
{
    public Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        dbContext.Set<Customer>().SingleOrDefaultAsync(customer => customer.Email == email, cancellationToken);

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Customer>().SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    public Task<Customer?> GetByVerificationTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.Set<Customer>().SingleOrDefaultAsync(customer => customer.VerificationTokenHash == tokenHash, cancellationToken);

    public Task<Customer?> GetByResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.Set<Customer>().SingleOrDefaultAsync(customer => customer.PasswordResetTokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roles = await dbContext.Set<UserRole>()
            .Where(userRole => userRole.UserId == userId)
            .Join(
                dbContext.Set<Role>(),
                userRole => userRole.RoleId,
                role => role.Id,
                (_, role) => role.Name)
            .ToListAsync(cancellationToken);

        return roles;
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var permissions = await dbContext.Set<UserRole>()
            .Where(userRole => userRole.UserId == userId)
            .Join(
                dbContext.Set<Role>(),
                userRole => userRole.RoleId,
                role => role.Id,
                (_, role) => role)
            .SelectMany(role => role.Permissions, (_, permission) => permission.PermissionCode)
            .Distinct()
            .ToListAsync(cancellationToken);

        return permissions;
    }

    public async Task<IReadOnlyList<Customer>> SearchAsync(
        string? email,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Customer>()
            .Where(customer => !customer.IsDeleted);

        if (!string.IsNullOrWhiteSpace(email))
        {
            var term = email.Trim().ToLowerInvariant();
            query = query.Where(customer => customer.Email.ToLower().Contains(term));
        }

        var customers = await query
            .OrderBy(customer => customer.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return customers;
    }

    public async Task<int> CountAsync(string? email, CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Customer>()
            .Where(customer => !customer.IsDeleted);

        if (!string.IsNullOrWhiteSpace(email))
        {
            var term = email.Trim().ToLowerInvariant();
            query = query.Where(customer => customer.Email.ToLower().Contains(term));
        }

        return await query.CountAsync(cancellationToken);
    }

    public void Add(Customer customer) => dbContext.Set<Customer>().Add(customer);

    public void AddRole(UserRole userRole) => dbContext.Set<UserRole>().Add(userRole);
}
