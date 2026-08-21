namespace ECommerce.Infrastructure.Vault;

public sealed class VaultOptions
{
    public const string SectionName = "Vault";

    public string Address { get; set; } = "http://localhost:8200";

    public string Token { get; set; } = string.Empty;

    public string MountPath { get; set; } = "secret";

    public int CacheTtlSeconds { get; set; } = 300;

    public bool Enabled { get; set; }
}
