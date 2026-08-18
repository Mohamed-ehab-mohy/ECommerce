using ECommerce.Domain.Identity;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Identity.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Identity;

public sealed class MfaSecretRepository(ECommerceDbContext dbContext) : IMfaSecretRepository
{
    public async Task<MfaSecret?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken) =>
        await dbContext.Set<MfaSecret>().FirstOrDefaultAsync(m => m.CustomerId == customerId, cancellationToken);

    public void Add(MfaSecret mfaSecret) => dbContext.Set<MfaSecret>().Add(mfaSecret);
}
