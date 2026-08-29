using System.Security.Cryptography;
using System.Text;

namespace ECommerce.UseCases.Identity;

internal static class PkceHelper
{
    public static string S256Challenge(string codeVerifier)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier), hash);
        return Base64Url(hash);
    }

    public static bool Verify(string codeVerifier, string expectedChallenge, string method) =>
        string.Equals(method, "S256", StringComparison.Ordinal)
            ? FixedTimeEquals(S256Challenge(codeVerifier), expectedChallenge)
            : false;

    private static string Base64Url(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool FixedTimeEquals(string a, string b)
    {
        var left = Encoding.ASCII.GetBytes(a);
        var right = Encoding.ASCII.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(left, right);
    }
}
