using ECommerce.UseCases.Identity;

namespace ECommerce.UnitTests;

public sealed class VerificationTokensTests
{
    [Fact]
    public void Create_Returns_64_Hex_Characters()
    {
        var token = VerificationTokens.Create();

        Assert.Equal(64, token.Length);
        Assert.All(token, character => Assert.Contains(char.ToUpperInvariant(character), "0123456789ABCDEF"));
    }

    [Fact]
    public void Create_Is_Unique()
    {
        var first = VerificationTokens.Create();
        var second = VerificationTokens.Create();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Hash_Is_Deterministic()
    {
        var token = VerificationTokens.Create();

        Assert.Equal(VerificationTokens.Hash(token), VerificationTokens.Hash(token));
    }

    [Fact]
    public void Hash_Differs_For_Different_Tokens()
    {
        Assert.NotEqual(VerificationTokens.Hash("token-a"), VerificationTokens.Hash("token-b"));
    }
}
