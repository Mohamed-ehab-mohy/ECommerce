using ECommerce.Domain.Identity;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.Infrastructure.Identity;

public sealed class AddressRepository(ECommerceDbContext dbContext) : IAddressRepository
{
    public async Task<IReadOnlyList<CustomerAddress>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        await dbContext.Set<CustomerAddress>()
            .Where(address => address.CustomerId == customerId)
            .OrderBy(address => address.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<CustomerAddress?> GetByIdAndCustomerIdAsync(Guid id, Guid customerId, CancellationToken cancellationToken) =>
        dbContext.Set<CustomerAddress>()
            .SingleOrDefaultAsync(address => address.Id == id && address.CustomerId == customerId, cancellationToken);

    public void Add(CustomerAddress address) => dbContext.Set<CustomerAddress>().Add(address);

    public void Remove(CustomerAddress address) => dbContext.Set<CustomerAddress>().Remove(address);
}
