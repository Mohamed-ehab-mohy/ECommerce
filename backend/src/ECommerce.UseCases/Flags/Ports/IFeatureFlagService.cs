namespace ECommerce.UseCases.Flags.Ports;

public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string key, CancellationToken cancellationToken);

    Task<string?> GetDescriptionAsync(string key, CancellationToken cancellationToken);
}
