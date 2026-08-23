namespace ECommerce.Infrastructure.Identity;

public sealed class OAuthClient
{
    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public IReadOnlyList<string> AllowedScopes { get; init; } = [];

    public IReadOnlyList<string> AllowedGrantTypes { get; init; } = [];

    public IReadOnlyList<string> RedirectUris { get; init; } = [];

    public bool IsActive { get; init; } = true;
}
