using System.Security.Cryptography;
using System.Text;

namespace ECommerce.Domain.Audit;

public static class AuditChain
{
    public static string Compute(string? previousHash, string canonicalPayload)
    {
        var input = string.Concat(previousHash ?? string.Empty, "|", canonicalPayload);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    public static bool Verify(IReadOnlyList<AuditEntry> entries)
    {
        string? previousHash = null;

        foreach (var entry in entries.OrderBy(entry => entry.Id))
        {
            if (!string.Equals(entry.PreviousHash, previousHash, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(entry.Hash, Compute(previousHash, entry.CanonicalPayload()), StringComparison.Ordinal))
            {
                return false;
            }

            previousHash = entry.Hash;
        }

        return true;
    }
}
