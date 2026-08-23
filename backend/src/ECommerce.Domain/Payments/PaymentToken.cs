namespace ECommerce.Domain.Payments;

public sealed record PaymentToken(string ProviderKey, string Token)
{
    public override string ToString() => $"{ProviderKey}:{Mask(Token)}";

    private static string Mask(string token) =>
        token.Length <= 4 ? "****" : $"****{token[^4..]}";
}
