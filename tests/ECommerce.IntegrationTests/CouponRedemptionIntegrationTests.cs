using ECommerce.Domain.Inventory;
using ECommerce.Domain.Orders;
using ECommerce.Domain.Payments;
using ECommerce.Domain.Pricing;
using ECommerce.Infrastructure.Common;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Inventory;
using ECommerce.Infrastructure.Orders;
using ECommerce.Infrastructure.Outbox;
using ECommerce.Infrastructure.Payments;
using ECommerce.Infrastructure.Promotions;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Orders.Commands;
using ECommerce.UseCases.Orders.Handlers;
using ECommerce.UseCases.Orders.Responses;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using CheckoutAggregate = ECommerce.Domain.Orders.Checkout;

namespace ECommerce.IntegrationTests;

/// <summary>
/// QAS-02: coupon redemption is atomic. When the usage limit is N, exactly N concurrent
/// place-order attempts succeed; the remainder fail with COUPON_EXHAUSTED.
/// </summary>
public sealed class CouponRedemptionIntegrationTests : IClassFixture<PostgresContainerFixture>
{
    private const string Sku = "QAS-02";
    private const string CouponCode = "QAS02";
    private const int TotalUses = 10;

    private readonly PostgresContainerFixture _fixture;

    public CouponRedemptionIntegrationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Concurrent_Redemptions_Respect_Total_Uses_Exactly_N()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var utcNow = DateTime.UtcNow;
        var attemptCount = TotalUses * 2;

        var (promotionId, couponId, checkoutIds) = await SeedAsync(attemptCount, utcNow);

        var tasks = checkoutIds
            .Select((checkoutId, index) => PlaceAsync(checkoutId, $"key-qas02-{index}"))
            .ToArray();

        await Task.WhenAll(tasks);

        var succeeded = tasks.Count(task => task.Result.IsSuccess);
        var exhausted = tasks.Count(task => task.Result.IsFailure && task.Result.Error.Code == "COUPON_EXHAUSTED");

        Assert.Equal(TotalUses, succeeded);
        Assert.Equal(attemptCount - TotalUses, exhausted);

        await using (var verify = CreateContext())
        {
            var coupon = await verify.Coupons.SingleAsync(candidate => candidate.Id == couponId);
            Assert.Equal(TotalUses, coupon.UsedCount);

            var usages = await verify.CouponUsages.CountAsync(
                usage => usage.CouponId == couponId);
            Assert.Equal(TotalUses, usages);

            var placedCheckouts = await verify.Checkouts.CountAsync(
                candidate => candidate.AppliedCouponId == couponId && candidate.Status == CheckoutStatus.Placed);
            Assert.Equal(TotalUses, placedCheckouts);
        }
    }

    [SkippableFact]
    public async Task Per_Customer_Limit_Is_Enforced_Atomically()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var utcNow = DateTime.UtcNow;
        var (promotionId, couponId, checkoutIds) = await SeedAsync(
            TotalUses,
            utcNow,
            perCustomerLimit: 1,
            singleCustomer: true);

        var tasks = checkoutIds
            .Select((checkoutId, index) => PlaceAsync(checkoutId, $"key-qas02-cust-{index}"))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, tasks.Count(task => task.Result.IsSuccess));
        Assert.Equal(TotalUses - 1, tasks.Count(
            task => task.Result.IsFailure && task.Result.Error.Code == "COUPON_EXHAUSTED"));

        await using var verify = CreateContext();
        Assert.Equal(1, await verify.CouponUsages.CountAsync(usage => usage.CouponId == couponId));
        Assert.Equal(1, (await verify.Coupons.SingleAsync(candidate => candidate.Id == couponId)).UsedCount);
    }

    private async Task<(Guid PromotionId, Guid CouponId, IReadOnlyList<Guid> CheckoutIds)> SeedAsync(
        int attemptCount,
        DateTime utcNow,
        int? perCustomerLimit = null,
        bool singleCustomer = false)
    {
        await using var setup = CreateContext();
        await setup.Database.MigrateAsync();
        await setup.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE
                warehouses,
                stock_items,
                stock_movements,
                payments,
                payment_attempts,
                checkouts,
                orders,
                order_items,
                order_status_log,
                idempotency_keys,
                promotions,
                coupons,
                coupon_usages
            CASCADE;
            """);

        var warehouse = Warehouse.Create("W-QAS2", "QAS Warehouse", "Test", "UTC", WarehouseStatus.Active, utcNow);
        setup.Add(warehouse);
        var stockItem = StockItem.Create(Sku, warehouse.Id, utcNow);
        stockItem.Apply(
            StockMovement.Create(stockItem.Id, StockMovementType.Receipt, attemptCount, "seed", null, null, utcNow),
            utcNow);
        setup.Add(stockItem);

        var promotion = Promotion.Create(
            "QAS-02 discount",
            [],
            [new DiscountRule(DiscountType.Order, DiscountBasis.Amount, 5m, null)],
            StackingMatrix.BestOf,
            [],
            [],
            null,
            null,
            utcNow).Value;
        promotion.Activate(utcNow);
        setup.Add(promotion);

        var coupon = Coupon.Create(
            CouponCode,
            promotion.Id,
            TotalUses,
            perCustomerLimit,
            null,
            null,
            utcNow).Value;
        setup.Add(coupon);

        await setup.SaveChangesAsync();

        var customerId = Guid.NewGuid();
        var checkoutIds = new List<Guid>(attemptCount);

        for (var i = 0; i < attemptCount; i++)
        {
            var checkout = await SeedCheckoutAsync(
                promotion.Id,
                coupon.Id,
                singleCustomer ? customerId : Guid.NewGuid(),
                utcNow,
                i);
            checkoutIds.Add(checkout);
        }

        return (promotion.Id, coupon.Id, checkoutIds);
    }

    private async Task<Guid> SeedCheckoutAsync(
        Guid promotionId,
        Guid couponId,
        Guid customerId,
        DateTime utcNow,
        int index)
    {
        await using var context = CreateContext();

        var payment = Payment.Create(
            null, "mock", $"tok_qas02_{index}", $"tok_client_qas02_{index}", null, "USD", 19.90m, null, utcNow);
        payment.MarkAuthorized($"pi_qas02_{index}_auth", utcNow);
        context.Add(payment);

        var snapshot = new PriceSnapshot(
            [new PriceSnapshotItem(Guid.NewGuid(), Sku, "Widget", 20.00m, 15.00m, 1, null)],
            new TotalsSnapshot(15.00m, 0m, 0m, 9.90m, 0m, 24.90m));
        var address = new AddressSnapshot(
            "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");
        var checkout = CheckoutAggregate.Create(
            Guid.NewGuid(),
            customerId,
            $"customer{index}@example.com",
            "USD",
            snapshot,
            address,
            address,
            "standard",
            payment.Id,
            utcNow.AddMinutes(30),
            utcNow,
            couponId,
            [promotionId]);
        checkout.MarkPaymentAuthorized(utcNow);
        context.Add(checkout);

        await context.SaveChangesAsync();
        return checkout.Id;
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
            new CouponRepository(context),
            new UnitOfWork(context),
            TimeProvider.System,
            new PlaceOrderCommandValidator());

        return await handler.Handle(
            new PlaceOrderCommand(checkoutId, idempotencyKey),
            CancellationToken.None);
    }

    private ECommerceDbContext CreateContext()
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_fixture.ConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        var options = new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseNpgsql(dataSourceBuilder.Build())
            .AddInterceptors(new DomainEventsInterceptor())
            .Options;
        return new ECommerceDbContext(options);
    }
}
