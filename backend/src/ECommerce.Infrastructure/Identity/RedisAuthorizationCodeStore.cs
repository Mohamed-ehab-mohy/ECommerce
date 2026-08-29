using System.Security.Cryptography;
using System.Text.Json;
using ECommerce.UseCases.Identity.Ports;
using StackExchange.Redis;

namespace ECommerce.Infrastructure.Identity;

public sealed class RedisAuthorizationCodeStore(IConnectionMultiplexer redis) : IAuthorizationCodeStore
{
    private const string KeyPrefix = "oauth:code:";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDatabase _cache = redis.GetDatabase();

    public async Task<string> CreateAsync(AuthorizationCodeRecord record, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var code = GenerateCode();
        var key = $"{KeyPrefix}{code}";
        var stored = record with { Code = code };
        await _cache.StringSetAsync(key, JsonSerializer.Serialize(stored, JsonOptions), ttl);
        return code;
    }

    public async Task<AuthorizationCodeRecord?> ConsumeAsync(string code, CancellationToken cancellationToken)
    {
        var value = await _cache.StringGetDeleteAsync($"{KeyPrefix}{code}");
        return value.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<AuthorizationCodeRecord>(value.ToString(), JsonOptions);
    }

    private static string GenerateCode()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
