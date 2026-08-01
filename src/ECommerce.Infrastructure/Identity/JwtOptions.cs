namespace ECommerce.Infrastructure.Identity;

public sealed record JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "ecommerce-api";

    public string Audience { get; init; } = "ecommerce-client";

    public string? PrivateKeyPem { get; init; }

    public string? KeyFile { get; init; }

    public int AccessTokenTtlMinutes { get; init; } = 15;

    public int RefreshTokenTtlDays { get; init; } = 30;
}
