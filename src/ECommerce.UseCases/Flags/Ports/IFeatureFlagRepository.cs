using ECommerce.Domain.Flags;

namespace ECommerce.UseCases.Flags.Ports;

public interface IFeatureFlagRepository
{
    Task<FeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken);

    Task<IReadOnlyList<FeatureFlag>> ListAsync(CancellationToken cancellationToken);

    void Add(FeatureFlag flag);
}
