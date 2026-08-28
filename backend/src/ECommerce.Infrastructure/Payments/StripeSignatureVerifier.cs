using System.Security.Cryptography;
using System.Text;

namespace ECommerce.Infrastructure.Payments;

/// <summary>
/// Verifies the HMAC signature Stripe attaches to outbound webhook deliveries
/// (the <c>Stripe-Signature</c> header carrying <c>t=</c> and <c>v1=</c> values).
/// </summary>
public static class StripeSignatureVerifier
{
    /// <summary>Maximum accepted age (seconds) of the event timestamp in the signature header.</summary>
    public const int AllowedClockSkewSeconds = 300;

    public static bool Verify(string payload, string signatureHeader, string webhookSecret)
    {
        if (string.IsNullOrEmpty(webhookSecret))
        {
            return false;
        }

        var parts = signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries);
        string? timestamp = null;
        string? signature = null;

        foreach (var part in parts)
        {
            var equalsIndex = part.IndexOf('=');
            if (equalsIndex > 0 && equalsIndex < part.Length - 1)
            {
                var key = part[..equalsIndex];
                var value = part[(equalsIndex + 1)..];
                if (key == "t") timestamp = value;
                else if (key == "v1") signature = value;
            }
        }

        if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(signature))
        {
            return false;
        }

        if (!long.TryParse(timestamp, out var timestampUnix) ||
            Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestampUnix) > AllowedClockSkewSeconds)
        {
            return false;
        }

        var signedPayload = $"{timestamp}.{payload}";
        var secretBytes = webhookSecret.StartsWith("whsec_", StringComparison.Ordinal)
            ? Convert.FromBase64String(webhookSecret[6..])
            : Convert.FromBase64String(webhookSecret);

        using var hmac = new HMACSHA256(secretBytes);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));

        return CryptographicOperations.FixedTimeEquals(computedHash, Convert.FromHexString(signature));
    }
}
