using ECommerce.Domain.Inventory;
using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.Infrastructure.Common;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Inventory;
using ECommerce.Infrastructure.Orders;
using ECommerce.Infrastructure.Outbox;
using ECommerce.Infrastructure.Payments;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Orders.Commands;
using ECommerce.UseCases.Orders.Handlers;
using ECommerce.UseCases.Orders.Responses;
using Microsoft.EntityFrameworkCore;
using CheckoutAggregate = ECommerce.Domain.Orders.Checkout;

namespace ECommerce.IntegrationTests;

public sealed class OrderPlacementIntegrationTests : IClassFixture<PostgresContainerFixture>
{
    private const string Sku = "QAS-05";

    private readonly PostgresContainerFixture _fixture;

    public OrderPlacementIntegrationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Duplicate_Placement_Returns_Same_Order()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var checkoutId = await SeedAuthorizedCheckoutAsync();

        var first = await PlaceAsync(checkoutId, "key-duplicate");
        var second = await PlaceAsync(checkoutId, "key-duplicate");

        Assert.True(first.IsSuccess, first.Error.Description);
        Assert.True(second.IsSuccess, second.Error.Description);
        Assert.Equal(first.Value.OrderId, second.Value.OrderId);

        await using (var verify = CreateContext())
        {
            Assert.Equal(1, await verify.Orders.CountAsync());
            Assert.Equal(1, await verify.IdempotencyKeys.CountAsync());

            var checkout = await verify.Checkouts.SingleAsync(candidate => candidate.Id == checkoutId);
            Assert.Equal(CheckoutStatus.Placed, checkout.Status);
            Assert.NotNull(checkout.PlacedAt);

            var order = await verify.Orders.Include(candidate => candidate.Items).SingleAsync();
            Assert.Equal(checkoutId, order.CheckoutId);
            Assert.Equal(first.Value.OrderId, order.Id);
            Assert.Single(order.Items);

            var payment = await verify.Payments.SingleAsync();
            Assert.Equal(order.Id, payment.OrderId);
            Assert.Equal(PaymentStatus.Authorized, payment.Status);

            var stockItem = await verify.StockItems.SingleAsync(candidate => candidate.Sku == Sku);
            Assert.Equal(1, stockItem.Allocated);
            Assert.Equal(0, stockItem.Available);
        }
    }

    [SkippableFact]
    public async Task Different_Key_On_Placed_Checkout_Returns_Conflict()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var checkoutId = await SeedAuthorizedCheckoutAsync();

        var first = await PlaceAsync(checkoutId, "key-one");
        var second = await PlaceAsync(checkoutId, "key-two");

        Assert.True(first.IsSuccess, first.Error.Description);
        Assert.True(second.IsFailure);
        Assert.Equal(CheckoutErrors.InvalidState, second.Error);

        await using var verify = CreateContext();
        Assert.Equal(1, await verify.Orders.CountAsync());
        Assert.Equal(CheckoutStatus.Placed, (await verify.Checkouts.SingleAsync(candidate => candidate.Id == checkoutId)).Status);
    }

    [SkippableFact]
    public async Task Concurrent_Same_Key_Placements_Create_One_Order()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        const int attempts = 10;
        var checkoutId = await SeedAuthorizedCheckoutAsync();

        var tasks = Enumerable.Range(0, attempts)
            .Select(_ => PlaceAsync(checkoutId, "key-concurrent"))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.All(tasks, task =>
        {
            Assert.True(task.Result.IsSuccess, task.Result.Error.Description);
            Assert.Equal(tasks[0].Result.Value.OrderId, task.Result.Value.OrderId);
        });

        await using (var verify = CreateContext())
        {
            Assert.Equal(1, await verify.Orders.CountAsync());
            Assert.Equal(1, await verify.IdempotencyKeys.CountAsync());

            var stockItem = await verify.StockItems.SingleAsync(candidate => candidate.Sku == Sku);
            Assert.Equal(1, stockItem.Allocated);
        }
    }

    private async Task<Guid> SeedAuthorizedCheckoutAsync()
    {
        var utcNow = DateTime.UtcNow;

        await using (var setup = CreateContext())
        {
            await setup.Database.MigrateAsync();

            var warehouse = Warehouse.Create("W-QAS", "QAS Warehouse", "Test", "UTC", WarehouseStatus.Active, utcNow);
            setup.Add(warehouse);
            var stockItem = StockItem.Create(Sku, warehouse.Id, utcNow);
            stockItem.Apply(
                StockMovement.Create(stockItem.Id, StockMovementType.Receipt, 1, "seed", null, null, utcNow),
                utcNow);
            setup.Add(stockItem);

            var payment = Payment.Create(
                null, "mock", "tok_seed", "tok_client_seed", null, "USD", 24.90m, null, utcNow);
            payment.MarkAuthorized("pi_seed_auth", utcNow);
            setup.Add(payment);

            var snapshot = new PriceSnapshot(
                [new PriceSnapshotItem(Guid.NewGuid(), Sku, "Widget", 20.00m, 15.00m, 1, null)],
                new TotalsSnapshot(15.00m, 0m, 0m, 9.90m, 0m, 24.90m));
            var address = new AddressSnapshot(
                "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");
            var checkout = CheckoutAggregate.Create(
                Guid.NewGuid(),
                null,
                "ahmed@example.com",
                "USD",
                snapshot,
                address,
                address,
                "standard",
                payment.Id,
                utcNow.AddMinutes(30),
                utcNow);
            checkout.MarkPaymentAuthorized(utcNow);
            setup.Add(checkout);

            await setup.SaveChangesAsync();
            return checkout.Id;
        }
    }

    private async Task<Result<OrderResponse>> PlaceAsync(Guid checkoutId, string idempotencyKey)
    {
        await using var context = CreateContext();
        var handler = new PlaceOrderCommandHandler(
            new CheckoutRepository(context),
            new PaymentRepository(context),
            new OrderRepository(context),
            new IdempotencyKeyRepository(context),
            new StockAllocator(context, new StockRepository(context)),
            new OrderNumberGenerator(context),
            new UnitOfWork(context),
            TimeProvider.System,
            new PlaceOrderCommandValidator());

        return await handler.Handle(
            new PlaceOrderCommand(checkoutId, idempotencyKey),
            CancellationToken.None);
    }

    private ECommerceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .AddInterceptors(new DomainEventsInterceptor())
            .Options;
        return new ECommerceDbContext(options);
    }
}
