using ECommerce.Domain.Inventory;
using ECommerce.Infrastructure.Common;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Inventory;
using ECommerce.Infrastructure.Outbox;
using ECommerce.UseCases.Inventory.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ECommerce.IntegrationTests;

[Collection("Integration")]
public sealed class StockAllocationIntegrationTests : IClassFixture<IntegrationFixture>
{
    private const string Sku = "QAS-01";
    private const int AvailableUnits = 10;
    private const int ConcurrentRequests = 25;

    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IntegrationFixture _fixture;

    public StockAllocationIntegrationTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task No_Oversell_Under_Concurrent_Allocation()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await IntegrationFixture.EnsureDatabaseReadyAsync();
        using (TenantScope.Begin(TenantId))
        {
            await using var setup = CreateContext();
            var utcNow = DateTime.UtcNow;
            var warehouse = Warehouse.Create("W-QAS", "QAS Warehouse", "Test", "UTC", WarehouseStatus.Active, utcNow);
            setup.Add(warehouse);
            var stockItem = StockItem.Create(Sku, warehouse.Id, utcNow);
            stockItem.Apply(
                StockMovement.Create(stockItem.Id, StockMovementType.Receipt, AvailableUnits, "seed", null, null, utcNow),
                utcNow);
            setup.Add(stockItem);
            await setup.SaveChangesAsync();
        }

        var tasks = Enumerable.Range(0, ConcurrentRequests)
            .Select(_ => Task.Run(AllocateOneAsync))
            .ToArray();

        await Task.WhenAll(tasks);

        var succeeded = tasks.Count(task => task.Result.HasShortfalls is false);
        var failed = tasks.Count(task => task.Result.HasShortfalls);

        Assert.Equal(AvailableUnits, succeeded);
        Assert.Equal(ConcurrentRequests - AvailableUnits, failed);

        using (TenantScope.Begin(TenantId))
        {
            await using var verify = CreateContext();
            var item = await verify.StockItems.SingleAsync(stockItem => stockItem.Sku == Sku);
            Assert.Equal(AvailableUnits, item.OnHand);
            Assert.Equal(AvailableUnits, item.Allocated);
            Assert.Equal(0, item.Available);
            Assert.Equal(AvailableUnits, await verify.StockMovements.CountAsync(
                movement => movement.StockItemId == item.Id && movement.Type == StockMovementType.Allocate));
        }
    }

    private async Task<StockAllocationResult> AllocateOneAsync()
    {
        var utcNow = DateTime.UtcNow;
        using var scope = TenantScope.Begin(TenantId);
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var allocator = new StockAllocator(context, new StockRepository(context));

        var result = await allocator.AllocateAsync(
            [new AllocationRequestItem(Sku, 1)],
            "order",
            Guid.NewGuid().ToString("N"),
            utcNow,
            CancellationToken.None);

        if (result.HasShortfalls)
        {
            await transaction.RollbackAsync();
        }
        else
        {
            await transaction.CommitAsync();
        }

        return result;
    }

    private ECommerceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseNpgsql(_fixture.PostgresConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .AddInterceptors(new DomainEventsInterceptor())
            .AddInterceptors(new TenantAwareSaveChangesInterceptor())
            .Options;
        return new ECommerceDbContext(options);
    }
}
