using ECommerce.Domain.Flags;
using ECommerce.Infrastructure.Data;
using ECommerce.UseCases.Flags.Ports;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Flags;

public sealed class FeatureFlagRepository(ECommerceDbContext dbContext) : IFeatureFlagRepository
{
    public Task<FeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken) =>
        dbContext.Set<FeatureFlag>()
            .SingleOrDefaultAsync(flag => flag.Key == key, cancellationToken);

    public Task<IReadOnlyList<FeatureFlag>> ListAsync(CancellationToken cancellationToken) =>
        dbContext.Set<FeatureFlag>()
            .OrderBy(flag => flag.Key)
            .ToListAsync(cancellationToken)
            .ContinueWith(
                task => (IReadOnlyList<FeatureFlag>)task.Result,
                cancellationToken);

    public void Add(FeatureFlag flag) => dbContext.Set<FeatureFlag>().Add(flag);
}
