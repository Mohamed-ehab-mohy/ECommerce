using ECommerce.Domain.Orders;
using ECommerce.UseCases.Fulfillment.Commands;
using ECommerce.UseCases.Fulfillment.Handlers;

namespace ECommerce.UnitTests;

public sealed class CorrectShippingAddressCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 13, 18, 0, 0, DateTimeKind.Utc);

    private readonly FakeOrderRepository _orders = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private CorrectShippingAddressCommandHandler Handler =>
        new(
            _orders,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            new CorrectShippingAddressCommandValidator());

    private Order SeedPlacedOrder()
    {
        var snapshot = new PriceSnapshot(
            [new PriceSnapshotItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 2, null)],
            new TotalsSnapshot(30.00m, 0m, 0m, 9.90m, 0m, 39.90m));

        var order = Order.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, "ahmed@example.com", "USD",
            "E-20260813-0001", snapshot,
            new AddressSnapshot("Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000"),
            new AddressSnapshot("Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000"),
            "standard", Guid.NewGuid(), UtcNow);

        _orders.Add(order);
        return order;
    }

    private static CorrectShippingAddressCommand CreateCommand(Guid orderId) =>
        new(orderId, "Mona Ali", "0507654321", "2 Marina Walk", "Abu Dhabi", null, "AE", "00001");

    [Fact]
    public async Task Correct_Updates_Shipping_Address()
    {
        var order = SeedPlacedOrder();

        var result = await Handler.Handle(CreateCommand(order.Id), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal("Mona Ali", order.ShippingAddress.FullName);
        Assert.Equal("2 Marina Walk", order.ShippingAddress.Street);
        Assert.Equal("Abu Dhabi", order.ShippingAddress.City);
        Assert.Null(order.ShippingAddress.Region);
        Assert.Equal("AE", order.ShippingAddress.Country);
        Assert.Equal("00001", order.ShippingAddress.PostalCode);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Correct_Unknown_Order_Returns_NotFound()
    {
        var result = await Handler.Handle(CreateCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.OrderNotFound, result.Error);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Correct_Shipped_Order_Returns_AddressCorrectionNotAllowed()
    {
        var order = SeedPlacedOrder();
        var productId = order.Items.Single().ProductId;
        order.MarkBackordered([(productId, "SKU-1", 2)], UtcNow);
        order.FillBackorderItems("SKU-1", 2, UtcNow);
        order.StartFulfillment("user", null, null, UtcNow);
        order.MarkPacked("user", null, null, UtcNow);
        order.Ship("dhl", ["TRK-1"], "user", null, null, UtcNow);

        var result = await Handler.Handle(CreateCommand(order.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.AddressCorrectionNotAllowed, result.Error);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Correct_Invalid_Address_Returns_Validation_Failure()
    {
        var order = SeedPlacedOrder();

        var result = await Handler.Handle(
            new CorrectShippingAddressCommand(order.Id, "", null, "", "Dubai", null, "AE", "00001"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
