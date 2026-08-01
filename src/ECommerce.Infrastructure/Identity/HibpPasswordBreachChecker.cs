using System.Security.Cryptography;
using System.Text;
using ECommerce.UseCases.Identity.Ports;

namespace ECommerce.Infrastructure.Identity;

public sealed class HibpPasswordBreachChecker(HttpClient httpClient) : IPasswordBreachChecker
{
    private static readonly Uri RangeEndpoint = new("https://api.pwnedpasswords.com/range/");

    public async Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken)
    {
        var sha1 = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
        var prefix = sha1[..5];
        var suffix = sha1[5..];

        var response = await httpClient.GetStringAsync(new Uri(RangeEndpoint, prefix), cancellationToken);

        return response
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Any(line => line.StartsWith(suffix + ":", StringComparison.OrdinalIgnoreCase));
    }
}
