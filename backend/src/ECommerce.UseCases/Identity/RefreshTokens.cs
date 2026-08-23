using System.Security.Cryptography;
using System.Text;

namespace ECommerce.UseCases.Identity;

public static class RefreshTokens
{
    public static string Create() =>
        $"r_{Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_')}";

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
