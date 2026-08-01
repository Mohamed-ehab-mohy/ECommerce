using System.Security.Cryptography;
using System.Text;

namespace ECommerce.UseCases.Identity;

public static class VerificationTokens
{
    public static string Create() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
