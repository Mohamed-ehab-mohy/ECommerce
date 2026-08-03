using ECommerce.Domain.Cart;
using ECommerce.Infrastructure.Carts;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace ECommerce.IntegrationTests;

public sealed class CartRepositoryIntegrationTests :
    IClassFixture<PostgresContainerFixture>,
    IClassFixture<RedisContainerFixture>
{
    private readonly PostgresContainerFixture _postgres;
    private readonly RedisContainerFixture _redis;

    public CartRepositoryIntegrationTests(PostgresContainerFixture postgres, RedisContainerFixture redis)
    {
        _postgres = postgres;
        _redis = redis;
    }

    [SkippableFact]
    public async Task Cart_Survives_New_Repository_Instance_And_Mutation_Updates_Cache_And_Store()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var previous = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _postgres.ConnectionString);
        try
        {
            var utcNow = DateTime.UtcNow;
            var productId = Guid.NewGuid();

            await using (var setup = CreateContext())
            {
                await setup.Database.MigrateAsync();
            }

            await using (var redis = await ConnectRedisAsync())
            {
                var repository = CreateRepository(redis);

                var cart = Cart.Create("anon-key-1", "USD", utcNow.AddDays(30), utcNow);
                var addResult = cart.AddItem(productId, "SKU-C1", "Widget", 12.50m, 2, null, utcNow);
                Assert.True(addResult.IsSuccess);

                await repository.SaveAsync(cart, CancellationToken.None);
            }

            var cacheKey = new RedisKey($"cart:anon-key-1");
            await using (var connection = await ConnectionMultiplexer.ConnectAsync(_redis.ConnectionString))
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
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", previous);
        }
    }

    [SkippableFact]
    public async Task Cache_Is_Invalidated_On_Mutation()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var previous = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _postgres.ConnectionString);
        try
        {
            var utcNow = DateTime.UtcNow;
            var productId = Guid.NewGuid();

            await using (var setup = CreateContext())
            {
                await setup.Database.MigrateAsync();
            }

            await using (var redis = await ConnectRedisAsync())
            {
                var repository = CreateRepository(redis);

                var cart = Cart.Create("anon-key-2", "USD", utcNow.AddDays(30), utcNow);
                cart.AddItem(productId, "SKU-C2", "Widget", 12.50m, 2, null, utcNow);
                await repository.SaveAsync(cart, CancellationToken.None);
            }

            await using (var redis = await ConnectRedisAsync())
            {
                var mutated = CreateRepository(redis);
                var cart = await mutated.ByOwnerKeyAsync("anon-key-2", CancellationToken.None);
                Assert.NotNull(cart);
                cart.AddItem(Guid.NewGuid(), "SKU-C3", "Gadget", 5.00m, 1, null, utcNow);
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
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", previous);
        }
    }

    [SkippableFact]
    public async Task ByOwnerKey_Returns_Null_For_Unknown_Cart()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var previous = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _postgres.ConnectionString);
        try
        {
            await using var context = CreateContext();
            await context.Database.MigrateAsync();

            await using var redis = await ConnectRedisAsync();
            var repository = CreateRepository(redis);
            var cart = await repository.ByOwnerKeyAsync("missing-key", CancellationToken.None);
            Assert.Null(cart);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", previous);
        }
    }

    private ECommerceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseNpgsql(_postgres.ConnectionString)
            .AddInterceptors(new DomainEventsInterceptor())
            .Options;
        return new ECommerceDbContext(options);
    }

    private async Task<IConnectionMultiplexer> ConnectRedisAsync() =>
        await ConnectionMultiplexer.ConnectAsync(_redis.ConnectionString);

    private CartRepository CreateRepository(IConnectionMultiplexer redis) =>
        new(
            CreateContext(),
            redis,
            NullLogger<CartRepository>.Instance);
}
