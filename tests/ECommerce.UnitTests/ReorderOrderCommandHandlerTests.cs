using ECommerce.Domain.Catalog;
using ECommerce.Domain.Cart;
using ECommerce.Domain.Inventory;
using ECommerce.Domain.Orders;
using ECommerce.UseCases.Checkout.Services;
using ECommerce.UseCases.Orders.Commands;
using ECommerce.UseCases.Orders.Handlers;
using ECommerce.UseCases.Pricing;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.UnitTests;

public sealed class ReorderOrderCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid WarehouseId = Guid.NewGuid();

    private static readonly AddressSnapshot Address = new(
        "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");

    private readonly FakeOrderRepository _orders = new();

    private readonly FakeProductRepository _products = new();

    private readonly FakeCartRepository _carts = new();

    private readonly FakeStockRepository _stock = new();

    private ReorderOrderCommandHandler CreateHandler() =>
        new(
            _orders,
            _products,
            _carts,
            new StockAvailabilityVerifier(_stock),
            new DefaultCurrencyCatalog(),
            new FixedTimeProvider(UtcNow),
            new ReorderOrderCommandValidator(),
            NullLogger<ReorderOrderCommandHandler>.Instance);

    private Product CreateActiveProduct(string sku, string name = "Widget")
    {
        var product = Product.Create(
            sku, sku.ToLowerInvariant().Replace(" ", "-"), "en", name, null,
            "USD", 20.00m, 15.00m, null, null, false, ProductStatus.Active, UtcNow);
        _products.Add(product);
        return product;
    }

    private Order CreatePlacedOrder(Guid customerId, Product product, int quantity = 2)
    {
        var snapshot = new PriceSnapshot(
            [new PriceSnapshotItem(product.Id, product.Sku, "Widget", 20.00m, 15.00m, quantity, null)],
            new TotalsSnapshot(30.00m, 10.00m, 0m, 9.90m, 0m, 39.90m, 0m));

        var order = Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            customerId,
            "ahmed@example.com",
            "USD",
            "E-20260807-000001",
            snapshot,
            Address,
            Address,
            "standard",
            Guid.NewGuid(),
            UtcNow);
        _orders.Add(order);
        return order;
    }

    private void SeedStock(string sku, int onHand)
    {
        var item = StockItem.Create(sku, WarehouseId, UtcNow);
        item.Apply(StockMovement.Create(item.Id, StockMovementType.Receipt, onHand, "RECV", null, null, UtcNow), UtcNow);
        _stock.Items.Add(item);
    }

    [Fact]
    public async Task Handle_Copies_Order_Items_Into_Cart()
    {
        var customerId = Guid.NewGuid();
        var product = CreateActiveProduct("SKU-1");
        var order = CreatePlacedOrder(customerId, product);
        SeedStock("SKU-1", 10);

        var result = await CreateHandler().Handle(
            new ReorderOrderCommand(order.OrderNumber, customerId),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(product.Id, item.ProductId);
        Assert.Equal("SKU-1", item.Sku);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public async Task Handle_Rejects_Other_Customer()
    {
        var product = CreateActiveProduct("SKU-1");
        var order = CreatePlacedOrder(Guid.NewGuid(), product);
        SeedStock("SKU-1", 10);

        var result = await CreateHandler().Handle(
            new ReorderOrderCommand(order.OrderNumber, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.NotYourOrder, result.Error);
    }

    [Fact]
    public async Task Handle_Rejects_Inactive_Product()
    {
        var customerId = Guid.NewGuid();
        var product = Product.Create(
            "SKU-1", "sku-1", "en", "Widget", null,
            "USD", 20.00m, 15.00m, null, null, false, ProductStatus.Inactive, UtcNow);
        _products.Add(product);
        var order = CreatePlacedOrder(customerId, product);
        SeedStock("SKU-1", 10);

        var result = await CreateHandler().Handle(
            new ReorderOrderCommand(order.OrderNumber, customerId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.ProductInactive, result.Error);
    }

    [Fact]
    public async Task Handle_Rejects_Insufficient_Stock()
    {
        var customerId = Guid.NewGuid();
        var product = CreateActiveProduct("SKU-1");
        var order = CreatePlacedOrder(customerId, product, quantity: 5);
        SeedStock("SKU-1", 2);

        var result = await CreateHandler().Handle(
            new ReorderOrderCommand(order.OrderNumber, customerId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ERR_STK_001", result.Error.Code);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
