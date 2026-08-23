using ECommerce.Infrastructure.Integrations;

namespace ECommerce.UnitTests;

public sealed class HmacWebhookSignerTests
{
    private const string Secret = "webhook-secret";
    private const string Payload = "{\"type\":\"order.placed\"}";

    private static readonly string Expected =
        "sha256=553ced41f348a77a1a41dbd821a5ffa2c07998c39d9d989ead0a6a38b972583d";

    private readonly HmacWebhookSigner _signer = new();

    [Fact]
    public void ComputeSignature_Matches_Known_Hmac_Vector()
    {
        var signature = _signer.ComputeSignature(Secret, Payload);

        Assert.Equal(Expected, signature);
    }

    [Fact]
    public void ComputeSignature_Uses_Lowercase_Hex_Prefix()
    {
        var signature = _signer.ComputeSignature(Secret, Payload);

        Assert.StartsWith("sha256=", signature);
        Assert.Equal(signature, signature.ToLowerInvariant());
        Assert.Matches("^sha256=[0-9a-f]{64}$", signature);
    }

    [Fact]
    public void ComputeSignature_Is_Secret_Dependent()
    {
        var other = _signer.ComputeSignature("another-secret", Payload);

        Assert.NotEqual(Expected, other);
    }
}
