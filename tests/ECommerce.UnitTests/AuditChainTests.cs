using ECommerce.Domain.Audit;
using ECommerce.Shared.Audit;

namespace ECommerce.UnitTests;

public sealed class AuditChainTests
{
    private static readonly DateTime Timestamp = new(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);

    private static AuditEntry CreateEntry(string action, string? previousHash = null) =>
        AuditEntry.Create(
            Guid.NewGuid(),
            AuditActorType.User,
            action,
            "Customer",
            Guid.NewGuid().ToString(),
            null,
            """{"displayName":"Ahmed"}""",
            "203.0.113.1",
            "test-agent",
            "trace-1",
            previousHash,
            Timestamp);

    [Fact]
    public void Compute_Is_Deterministic_And_Stable()
    {
        var first = AuditChain.Compute(null, "payload");
        var second = AuditChain.Compute(null, "payload");

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
        Assert.NotEqual(AuditChain.Compute(null, "payload"), AuditChain.Compute(null, "payload2"));
    }

    [Fact]
    public void Compute_Depends_On_Previous_Hash()
    {
        var withPrev = AuditChain.Compute("ABC", "payload");
        var withoutPrev = AuditChain.Compute(null, "payload");

        Assert.NotEqual(withPrev, withoutPrev);
    }

    [Fact]
    public void Verify_Returns_True_For_Linked_Chain()
    {
        var first = CreateEntry("identity.login");
        var second = CreateEntry("identity.profile.updated", first.Hash);
        var third = CreateEntry("identity.address.added", second.Hash);

        Assert.True(AuditChain.Verify([first, second, third]));
        Assert.True(AuditChain.Verify([first]));
        Assert.True(AuditChain.Verify([]));
    }

    [Fact]
    public void Verify_Detects_Broken_Link_When_Previous_Hash_Is_Wrong()
    {
        var first = CreateEntry("identity.login");
        var second = CreateEntry("identity.profile.updated", "wrong-previous");

        Assert.False(AuditChain.Verify([first, second]));
    }

    [Fact]
    public void Verify_Detects_Content_Tampering_When_Hash_Not_Updated()
    {
        var first = CreateEntry("identity.login");
        var second = CreateEntry("identity.profile.updated", first.Hash);
        var third = CreateEntry("identity.address.added", second.Hash);

        typeof(AuditEntry).GetProperty(nameof(AuditEntry.After))!.SetValue(third, """{"street":"HACKED"}""");

        Assert.False(AuditChain.Verify([first, second, third]));
    }
}
