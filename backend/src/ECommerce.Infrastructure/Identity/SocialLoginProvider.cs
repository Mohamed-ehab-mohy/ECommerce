namespace ECommerce.Infrastructure.Identity;

public interface ISocialLoginProvider
{
    Task<SocialUserInfo?> ValidateTokenAsync(string provider, string token, CancellationToken ct);
}

public sealed record SocialUserInfo(string Provider, string ProviderSubject, string? Email, string? DisplayName);

public sealed class StubSocialLoginProvider : ISocialLoginProvider
{
    public Task<SocialUserInfo?> ValidateTokenAsync(string provider, string token, CancellationToken ct)
    {
        return Task.FromResult<SocialUserInfo?>(null);
    }
}
