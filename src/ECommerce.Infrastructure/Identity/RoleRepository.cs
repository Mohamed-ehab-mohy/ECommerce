using ECommerce.Domain.Identity;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.Infrastructure.Identity;

public sealed class RoleRepository(ECommerceDbContext dbContext) : IRoleRepository
{
    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<Role>()
            .Include(role => role.Permissions)
            .SingleOrDefaultAsync(role => role.Id == id && !role.IsDeleted, cancellationToken);

    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken) =>
        dbContext.Set<Role>()
            .Include(role => role.Permissions)
            .SingleOrDefaultAsync(role => role.Name == name && !role.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken)
    {
        var roles = await dbContext.Set<Role>()
            .Where(role => !role.IsDeleted)
            .OrderBy(role => role.Name)
            .Include(role => role.Permissions)
            .ToListAsync(cancellationToken);

        return roles;
    }

    public void Add(Role role) => dbContext.Set<Role>().Add(role);
}
