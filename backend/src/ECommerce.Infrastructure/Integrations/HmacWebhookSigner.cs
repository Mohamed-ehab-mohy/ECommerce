using System.Security.Cryptography;
using System.Text;
using ECommerce.UseCases.Integrations.Ports;

namespace ECommerce.Infrastructure.Integrations;

/// <summary>
/// Computes the <c>sha256=&lt;hex&gt;</c> signature over the raw payload with the endpoint secret
/// (HMAC-SHA256, docs/08 §8.1).
/// </summary>
public sealed class HmacWebhookSigner : IWebhookSigner
{
    public string ComputeSignature(string secret, string payload)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);

        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
