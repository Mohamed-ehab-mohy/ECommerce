using ECommerce.Domain.Cart;
using ECommerce.Infrastructure.Carts;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace ECommerce.IntegrationTests;

public sealed class CartRepositoryIntegrationTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public CartRepositoryIntegrationTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Cart_Survives_New_Repository_Instance_And_Mutation_Updates_Cache_And_Store()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var utcNow = DateTime.UtcNow;
        var productId = Guid.NewGuid();

        await IntegrationFixture.EnsureDatabaseReadyAsync();

        await using (var redis = await ConnectRedisAsync())
        {
            var repository = CreateRepository(redis);

            var cart = Cart.Create("anon-key-1", "USD", utcNow.AddDays(30), utcNow);
            var addResult = cart.AddItem(productId, "SKU-C1", "Widget", 12.50m, 12.50m, 2, null, utcNow);
            Assert.True(addResult.IsSuccess);

            await repository.SaveAsync(cart, CancellationToken.None);
        }

        var cacheKey = new RedisKey($"cart:anon-key-1");
        await using (var connection = await ConnectionMultiplexer.ConnectAsync(_fixture.RedisConnectionString))
        {
            var value = await connection.GetDatabase().StringGetAsync(cacheKey);
            Assert.False(value.IsNullOrEmpty, "cache entry should be written through on save");
        }

        await using (var redis = await ConnectRedisAsync())
        {
            var repository = CreateRepository(redis);
            var loaded = await repository.ByOwnerKeyAsync("anon-key-1", CancellationToken.None);
            Assert.NotNull(loaded);
            Assert.Equal("USD", loaded.Currency);
            var item = Assert.Single(loaded.Items);
            Assert.Equal(productId, item.ProductId);
            Assert.Equal(2, item.Quantity);
        }
    }

    [SkippableFact]
    public async Task Cache_Is_Invalidated_On_Mutation()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var utcNow = DateTime.UtcNow;
        var productId = Guid.NewGuid();

        await IntegrationFixture.EnsureDatabaseReadyAsync();

        await using (var redis = await ConnectRedisAsync())
        {
            var repository = CreateRepository(redis);

            var cart = Cart.Create("anon-key-2", "USD", utcNow.AddDays(30), utcNow);
            cart.AddItem(productId, "SKU-C2", "Widget", 12.50m, 12.50m, 2, null, utcNow);
            await repository.SaveAsync(cart, CancellationToken.None);
        }

        await using (var redis = await ConnectRedisAsync())
        {
            var mutated = CreateRepository(redis);
            var cart = await mutated.ByOwnerKeyAsync("anon-key-2", CancellationToken.None);
            Assert.NotNull(cart);
            cart.AddItem(Guid.NewGuid(), "SKU-C3", "Gadget", 5.00m, 5.00m, 1, null, utcNow);
            await mutated.SaveAsync(cart, CancellationToken.None);
        }

        await using (var redis = await ConnectRedisAsync())
        {
            var read = CreateRepository(redis);
            var cart = await read.ByOwnerKeyAsync("anon-key-2", CancellationToken.None);
            Assert.NotNull(cart);
            Assert.Equal(2, cart.Items.Count);
        }
    }

    [SkippableFact]
    public async Task ByOwnerKey_Returns_Null_For_Unknown_Cart()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        await IntegrationFixture.EnsureDatabaseReadyAsync();

        await using var redis = await ConnectRedisAsync();
        var repository = CreateRepository(redis);
        var cart = await repository.ByOwnerKeyAsync("missing-key", CancellationToken.None);
        Assert.Null(cart);
    }

    [SkippableFact]
    public async Task ListPrice_And_UnitPrice_Survive_Roundtrip()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var utcNow = DateTime.UtcNow;
        var productId = Guid.NewGuid();

        await IntegrationFixture.EnsureDatabaseReadyAsync();

        await using (var redis = await ConnectRedisAsync())
        {
            var repository = CreateRepository(redis);
            var cart = Cart.Create("anon-key-3", "USD", utcNow.AddDays(30), utcNow);
            cart.AddItem(productId, "SKU-C4", "Widget", 20.00m, 15.00m, 2, null, utcNow);
            await repository.SaveAsync(cart, CancellationToken.None);
        }

        await using (var redis = await ConnectRedisAsync())
        {
            var repository = CreateRepository(redis);
            var loaded = await repository.ByOwnerKeyAsync("anon-key-3", CancellationToken.None);
            Assert.NotNull(loaded);
            var item = Assert.Single(loaded.Items);
            Assert.Equal(20.00m, item.ListPrice);
            Assert.Equal(15.00m, item.UnitPrice);
        }
    }

    [SkippableFact]
    public async Task Save_With_Stale_Version_Throws_Concurrency_Conflict()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var utcNow = DateTime.UtcNow;
        var productId = Guid.NewGuid();

        await IntegrationFixture.EnsureDatabaseReadyAsync();

        await using (var redis = await ConnectRedisAsync())
        {
            var repository = CreateRepository(redis);
            var cart = Cart.Create("anon-key-4", "USD", utcNow.AddDays(30), utcNow);
            cart.AddItem(productId, "SKU-C5", "Widget", 10.00m, 10.00m, 1, null, utcNow);
            await repository.SaveAsync(cart, CancellationToken.None);
        }

        Cart firstCopy;
        Cart secondCopy;
        await using (var redis = await ConnectRedisAsync())
        {
            var repository = CreateRepository(redis);
            firstCopy = (await repository.ByOwnerKeyAsync("anon-key-4", CancellationToken.None))!;
            secondCopy = (await repository.ByOwnerKeyAsync("anon-key-4", CancellationToken.None))!;
        }

        await using (var redis = await ConnectRedisAsync())
        {
            var repository = CreateRepository(redis);
            firstCopy.AddItem(Guid.NewGuid(), "SKU-C6", "Gadget", 5.00m, 5.00m, 1, null, utcNow);
            await repository.SaveAsync(firstCopy, CancellationToken.None);
        }

        await using (var redis = await ConnectRedisAsync())
        {
            var repository = CreateRepository(redis);
            await Assert.ThrowsAsync<CartConcurrencyException>(() => repository.SaveAsync(secondCopy, CancellationToken.None));
        }
    }

    private ECommerceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseNpgsql(_fixture.PostgresConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .AddInterceptors(new DomainEventsInterceptor())
            .Options;
        return new ECommerceDbContext(options);
    }

    private async Task<IConnectionMultiplexer> ConnectRedisAsync() =>
        await ConnectionMultiplexer.ConnectAsync(_fixture.RedisConnectionString);

    private CartRepository CreateRepository(IConnectionMultiplexer redis) =>
        new(
            CreateContext(),
            redis,
            NullLogger<CartRepository>.Instance);
}
