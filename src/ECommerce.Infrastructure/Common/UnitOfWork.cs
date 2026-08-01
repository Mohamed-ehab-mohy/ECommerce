using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Common;

namespace ECommerce.Infrastructure.Common;

public sealed class UnitOfWork(ECommerceDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
