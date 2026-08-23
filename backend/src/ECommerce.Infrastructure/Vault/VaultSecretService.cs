using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.UseCases.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Vault;

public sealed class VaultSecretService(
    IHttpClientFactory httpClientFactory,
    IOptions<VaultOptions> options,
    ILogger<VaultSecretService> logger) : IVaultService
{
    private readonly ConcurrentDictionary<string, (string Data, DateTime ExpiresAt)> _cache = new();

    public async Task<string?> GetSecretAsync(string path, CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        if (!config.Enabled)
        {
            logger.LogDebug("Vault is disabled; returning null for path {Path}.", path);
            return null;
        }

        var cacheKey = $"kv:{path}";
        if (_cache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
        {
            return cached.Data;
        }

        try
        {
            var client = CreateClient();
            var url = $"{config.Address}/v1/{config.MountPath}/{path}";
            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var vaultResponse = await response.Content.ReadFromJsonAsync<VaultReadResponse>(cancellationToken: cancellationToken);
            var value = vaultResponse?.Data?.Data?.Values?.FirstOrDefault()?.ToString();

            if (value is not null)
            {
                _cache[cacheKey] = (value, DateTime.UtcNow.AddSeconds(config.CacheTtlSeconds));
            }

            return value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read secret from Vault at path {Path}.", path);
            return null;
        }
    }

    public async Task<Dictionary<string, string>> GetSecretDataAsync(string path, CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        if (!config.Enabled)
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var client = CreateClient();
            var url = $"{config.Address}/v1/{config.MountPath}/{path}";
            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var vaultResponse = await response.Content.ReadFromJsonAsync<VaultReadResponse>(cancellationToken: cancellationToken);

            var result = new Dictionary<string, string>();
            if (vaultResponse?.Data?.Data is not null)
            {
                foreach (var kvp in vaultResponse.Data.Data)
                {
                    result[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read secret data from Vault at path {Path}.", path);
            return new Dictionary<string, string>();
        }
    }

    public async Task SetSecretAsync(string path, Dictionary<string, string> data, CancellationToken cancellationToken = default)
    {
        var config = options.Value;
        if (!config.Enabled)
        {
            logger.LogDebug("Vault is disabled; skipping write for path {Path}.", path);
            return;
        }

        try
        {
            var client = CreateClient();
            var url = $"{config.Address}/v1/{config.MountPath}/{path}";
            var payload = new { data };
            using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            _cache.TryRemove($"kv:{path}", out _);
            logger.LogInformation("Secret written to Vault at path {Path}.", path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write secret to Vault at path {Path}.", path);
            throw;
        }
    }

    public Task<IDisposable> WithRenewableTokenAsync(string path, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var cts = new CancellationTokenSource();
        var token = ttl ?? TimeSpan.FromMinutes(30);

        _ = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                await Task.Delay(token / 2, cts.Token).ConfigureAwait(false);
                _cache.TryRemove($"kv:{path}", out _);
            }
        }, cts.Token);

        return Task.FromResult<IDisposable>(new RenewableTokenHandle(cts));
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("vault");
        var config = options.Value;
        if (!string.IsNullOrEmpty(config.Token))
        {
            client.DefaultRequestHeaders.Remove("X-Vault-Token");
            client.DefaultRequestHeaders.Add("X-Vault-Token", config.Token);
        }

        return client;
    }

    private sealed class RenewableTokenHandle(CancellationTokenSource cts) : IDisposable
    {
        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    private sealed class VaultReadResponse
    {
        public VaultReadData? Data { get; set; }
    }

    private sealed class VaultReadData
    {
        public Dictionary<string, object>? Data { get; set; }
    }
}
