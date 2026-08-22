using ECommerce.Domain.Orders;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Orders;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ECommerce.IntegrationTests;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class ReturnRequestIntegrationTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public ReturnRequestIntegrationTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<ECommerceDbContext> GetDbContextAsync()
    {
        await IntegrationFixture.EnsureDatabaseReadyAsync();
        return _fixture.DbContext;
    }

    [SkippableFact]
    public async Task Add_And_Get_ReturnRequest()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var dbContext = await GetDbContextAsync();
        var repo = new ReturnRequestRepository(dbContext);
        var items = new List<ReturnRequestItem>
        {
            ReturnRequestItem.Create(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", 1, 29.99m, null)
        };
        var rr = ReturnRequest.Create(Guid.NewGuid(), Guid.NewGuid(), "Test", "USD", 29.99m, false, items, DateTime.UtcNow);

        repo.Add(rr);
        await dbContext.SaveChangesAsync();

        var fetched = await repo.GetByIdAsync(rr.Id, CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal("Test", fetched.Reason);
        Assert.Single(fetched.Items);
    }

    [SkippableFact]
    public async Task ListPending_ReturnsOnlyRequested()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");
        var dbContext = await GetDbContextAsync();
        var repo = new ReturnRequestRepository(dbContext);
        var items = new List<ReturnRequestItem>
        {
            ReturnRequestItem.Create(Guid.NewGuid(), Guid.NewGuid(), "SKU-2", 1, 10m, null)
        };
        var rr = ReturnRequest.Create(Guid.NewGuid(), Guid.NewGuid(), "Pending", "USD", 10m, false, items, DateTime.UtcNow);
        repo.Add(rr);
        await dbContext.SaveChangesAsync();

        var pending = await repo.ListPendingAsync(1, 10, CancellationToken.None);
        Assert.Contains(pending, r => r.Id == rr.Id);
    }
}
