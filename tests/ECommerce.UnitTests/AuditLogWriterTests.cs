using ECommerce.Domain.Audit;
using ECommerce.Shared.Audit;
using ECommerce.UseCases.Audit;
using ECommerce.UseCases.Audit.Ports;

namespace ECommerce.UnitTests;

public sealed class AuditLogWriterTests
{
    [Fact]
    public async Task WriteAsync_Stores_Context_And_Serializes_Operations()
    {
        var entries = new FakeAuditEntryRepository();
        var context = new FakeAuditContextProvider(Guid.NewGuid(), "198.51.100.7");
        var writer = new AuditLogWriter(entries, context);

        await writer.WriteAsync(new AuditOperation(
            AuditActions.ProfileUpdated,
            "Customer",
            "customer-1",
            Before: new { displayName = "Old" },
            After: new { displayName = "New" }), CancellationToken.None);

        var entry = Assert.Single(entries.Entries);
        Assert.Equal(AuditActions.ProfileUpdated, entry.Action);
        Assert.Equal("Customer", entry.EntityType);
        Assert.Equal("customer-1", entry.EntityId);
        Assert.Equal(context.Get().ActorId, entry.ActorId);
        Assert.Equal(AuditActorType.User, entry.ActorType);
        Assert.Equal("198.51.100.7", entry.Ip);
        Assert.Equal("test-agent", entry.UserAgent);
        Assert.Equal("trace-1", entry.TraceId);
        Assert.Null(entry.PreviousHash);
        Assert.Contains("Old", entry.Before!);
        Assert.Contains("New", entry.After!);
        Assert.False(string.IsNullOrWhiteSpace(entry.Hash));
    }

    [Fact]
    public async Task WriteAsync_Links_Hashes_Into_Chain()
    {
        var entries = new FakeAuditEntryRepository();
        var writer = new AuditLogWriter(entries, new FakeAuditContextProvider());

        await writer.WriteAsync(new AuditOperation(AuditActions.Login, "Customer", "u1", After: new { userId = "u1" }), CancellationToken.None);
        await writer.WriteAsync(new AuditOperation(AuditActions.ProfileUpdated, "Customer", "u1"), CancellationToken.None);
        await writer.WriteAsync(new AuditOperation(AuditActions.AddressAdded, "CustomerAddress", "a1"), CancellationToken.None);

        Assert.Equal(3, entries.Entries.Count);
        Assert.Null(entries.Entries[0].PreviousHash);
        Assert.Equal(entries.Entries[0].Hash, entries.Entries[1].PreviousHash);
        Assert.Equal(entries.Entries[1].Hash, entries.Entries[2].PreviousHash);
        Assert.True(AuditChain.Verify(entries.Entries));
    }

    [Fact]
    public async Task WriteAsync_Uses_Explicit_Actor_For_Anonymous_Actions()
    {
        var entries = new FakeAuditEntryRepository();
        var writer = new AuditLogWriter(entries, new FakeAuditContextProvider());

        var actorId = Guid.NewGuid();
        await writer.WriteAsync(new AuditOperation(
            AuditActions.Login,
            "Customer",
            actorId.ToString(),
            ActorId: actorId,
            ActorType: AuditActorType.User), CancellationToken.None);

        var entry = Assert.Single(entries.Entries);
        Assert.Equal(actorId, entry.ActorId);
    }

    [Fact]
    public async Task WriteAsync_Omits_Null_Fields()
    {
        var entries = new FakeAuditEntryRepository();
        var writer = new AuditLogWriter(entries, new FakeAuditContextProvider());

        await writer.WriteAsync(new AuditOperation(AuditActions.AddressRemoved, "CustomerAddress", "a1"), CancellationToken.None);

        var entry = Assert.Single(entries.Entries);
        Assert.Null(entry.Before);
        Assert.Null(entry.After);
        Assert.True(AuditChain.Verify(entries.Entries));
    }
}
