using ECommerce.Domain.Orders;
using ECommerce.UseCases.Orders.Handlers;
using ECommerce.UseCases.Orders.Queries;

namespace ECommerce.UnitTests;

public sealed class GetOrderQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    private static readonly AddressSnapshot Address = new(
        "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");

    private static readonly PriceSnapshot Snapshot = new(
        [new PriceSnapshotItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 2, null)],
        new TotalsSnapshot(30.00m, 10.00m, 0m, 9.90m, 0m, 39.90m, 0m));

    private static Order CreateOrder(Guid customerId) =>
        Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            customerId,
            "ahmed@example.com",
            "USD",
            "E-20260807-000001",
            Snapshot,
            Address,
            Address,
            "standard",
            Guid.NewGuid(),
            Now);

    private static GetOrderQuery CreateQuery(
        string orderNumber,
        Guid? requesterCustomerId,
        bool supportAccess = false) =>
        new(orderNumber, requesterCustomerId, supportAccess);

    [Fact]
    public async Task Handle_Returns_Order_To_Owner()
    {
        var customerId = Guid.NewGuid();
        var repository = new FakeOrderRepository();
        repository.Add(CreateOrder(customerId));
        var handler = new GetOrderQueryHandler(repository);

        var result = await handler.Handle(
            CreateQuery("E-20260807-000001", customerId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("E-20260807-000001", result.Value.OrderNumber);
        Assert.Equal(customerId, result.Value.CustomerId);
        Assert.Single(result.Value.Timeline);
    }

    [Fact]
    public async Task Handle_Returns_NotYourOrder_To_Other_Customer()
    {
        var repository = new FakeOrderRepository();
        repository.Add(CreateOrder(Guid.NewGuid()));
        var handler = new GetOrderQueryHandler(repository);

        var result = await handler.Handle(
            CreateQuery("E-20260807-000001", Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.NotYourOrder, result.Error);
    }

    [Fact]
    public async Task Handle_Allows_Support_Access_For_Any_Customer()
    {
        var repository = new FakeOrderRepository();
        repository.Add(CreateOrder(Guid.NewGuid()));
        var handler = new GetOrderQueryHandler(repository);

        var result = await handler.Handle(
            CreateQuery("E-20260807-000001", Guid.NewGuid(), supportAccess: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_Returns_NotFound_For_Unknown_Order_Number()
    {
        var repository = new FakeOrderRepository();
        var handler = new GetOrderQueryHandler(repository);

        var result = await handler.Handle(
            CreateQuery("E-20260807-999999", Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.OrderNotFound, result.Error);
    }

    [Fact]
    public async Task Handle_Returns_NotFound_For_Malformed_Order_Number()
    {
        var repository = new FakeOrderRepository();
        var handler = new GetOrderQueryHandler(repository);

        var result = await handler.Handle(
            CreateQuery("NOT-A-NUMBER", Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.OrderNotFound, result.Error);
    }
}
