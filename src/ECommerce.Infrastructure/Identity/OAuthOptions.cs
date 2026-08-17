namespace ECommerce.Infrastructure.Identity;

public sealed class OAuthOptions
{
    public const string SectionName = "OAuth";

    public string Issuer { get; set; } = string.Empty;

    public string Authority { get; set; } = string.Empty;

    public List<OAuthClient> Clients { get; set; } = [];
}
