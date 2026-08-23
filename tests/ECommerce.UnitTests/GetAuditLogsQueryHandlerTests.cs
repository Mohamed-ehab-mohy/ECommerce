using ECommerce.Domain.Audit;
using ECommerce.UseCases.Audit;
using ECommerce.UseCases.Audit.Ports;
using ECommerce.UseCases.Audit.Queries;

namespace ECommerce.UnitTests;

public sealed class GetAuditLogsQueryHandlerTests
{
    private readonly FakeAuditEntryRepository _entries = new();

    private GetAuditLogsQueryHandler Handler => new(_entries, new GetAuditLogsQueryValidator());

    private Guid _alice;

    private async Task SeedAsync()
    {
        _alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var writer = new AuditLogWriter(_entries, new FakeAuditContextProvider());

        await writer.WriteAsync(new AuditOperation(
            AuditActions.Login, "Customer", _alice.ToString(), After: new { userId = _alice },
            ActorId: _alice), CancellationToken.None);
        await writer.WriteAsync(new AuditOperation(
            AuditActions.ProfileUpdated, "Customer", _alice.ToString(),
            ActorId: _alice), CancellationToken.None);
        await writer.WriteAsync(new AuditOperation(
            AuditActions.Login, "Customer", bob.ToString(), After: new { userId = bob },
            ActorId: bob), CancellationToken.None);
        await writer.WriteAsync(new AuditOperation(
            AuditActions.AddressAdded, "CustomerAddress", Guid.NewGuid().ToString(),
            ActorId: _alice), CancellationToken.None);
    }

    [Fact]
    public async Task Returns_Entries_Newest_First_With_Paging()
    {
        await SeedAsync();

        var result = await Handler.Handle(new GetAuditLogsQuery(Page: 1, PageSize: 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(AuditActions.AddressAdded, result.Value.Items[0].Action);
        Assert.Equal(AuditActions.Login, result.Value.Items[1].Action);
    }

    [Fact]
    public async Task Filters_By_Action()
    {
        await SeedAsync();

        var result = await Handler.Handle(new GetAuditLogsQuery(Action: AuditActions.Login), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.All(result.Value.Items, item => Assert.Equal(AuditActions.Login, item.Action));
    }

    [Fact]
    public async Task Filters_By_Actor()
    {
        await SeedAsync();

        var result = await Handler.Handle(new GetAuditLogsQuery(ActorId: _alice), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.All(result.Value.Items, item => Assert.Equal(_alice, item.ActorId));
    }

    [Fact]
    public async Task Rejects_Invalid_Paging()
    {
        var zeroPage = await Handler.Handle(new GetAuditLogsQuery(Page: 0), CancellationToken.None);
        Assert.True(zeroPage.IsFailure);
        Assert.Equal(ErrorType.Validation, zeroPage.Error.Type);

        var largePageSize = await Handler.Handle(new GetAuditLogsQuery(PageSize: 101), CancellationToken.None);
        Assert.True(largePageSize.IsFailure);
        Assert.Equal(ErrorType.Validation, largePageSize.Error.Type);
    }
}
