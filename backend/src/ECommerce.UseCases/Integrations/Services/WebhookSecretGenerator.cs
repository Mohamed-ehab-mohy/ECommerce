using System.Security.Cryptography;

namespace ECommerce.UseCases.Integrations.Services;

public static class WebhookSecretGenerator
{
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
