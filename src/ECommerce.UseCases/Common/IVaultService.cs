namespace ECommerce.UseCases.Common;

public interface IVaultService
{
    Task<string?> GetSecretAsync(string path, CancellationToken cancellationToken = default);

    Task<Dictionary<string, string>> GetSecretDataAsync(string path, CancellationToken cancellationToken = default);

    Task SetSecretAsync(string path, Dictionary<string, string> data, CancellationToken cancellationToken = default);

    Task<IDisposable> WithRenewableTokenAsync(string path, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
}
