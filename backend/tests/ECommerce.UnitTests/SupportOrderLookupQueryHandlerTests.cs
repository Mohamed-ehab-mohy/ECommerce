using ECommerce.Domain.Orders;
using ECommerce.UseCases.Orders.Handlers;
using ECommerce.UseCases.Orders.Queries;

namespace ECommerce.UnitTests;

public sealed class SupportOrderLookupQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    private static readonly AddressSnapshot Address = new(
        "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");

    private static readonly PriceSnapshot Snapshot = new(
        [new PriceSnapshotItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 2, null)],
        new TotalsSnapshot(30.00m, 10.00m, 0m, 9.90m, 0m, 39.90m, 0m));

    private static Order CreateOrder(Guid customerId, string email, string orderNumber, DateTime placedAt) =>
        Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            customerId,
            email,
            "USD",
            orderNumber,
            Snapshot,
            Address,
            Address,
            "standard",
            Guid.NewGuid(),
            placedAt);

    private static SupportOrderLookupQueryHandler CreateHandler(FakeOrderRepository repository) =>
        new(repository, new SupportOrderLookupQueryValidator());

    [Fact]
    public async Task Handle_Lookup_By_OrderNumber_Returns_Single_Order()
    {
        var repository = new FakeOrderRepository();
        repository.Add(CreateOrder(Guid.NewGuid(), "ahmed@example.com", "E-20260807-000001", Now));
        repository.Add(CreateOrder(Guid.NewGuid(), "sara@example.com", "E-20260807-000002", Now));
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new SupportOrderLookupQuery(OrderNumber: "E-20260807-000002"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Orders);
        Assert.Equal("E-20260807-000002", item.OrderNumber);
        Assert.Equal("s***@example.com", item.MaskedEmail);
    }

    [Fact]
    public async Task Handle_Lookup_By_Email_Returns_All_Matches_Masked()
    {
        var repository = new FakeOrderRepository();
        var customerId = Guid.NewGuid();
        repository.Add(CreateOrder(customerId, "ahmed@example.com", "E-20260807-000001", Now));
        repository.Add(CreateOrder(customerId, "ahmed@example.com", "E-20260807-000002", Now));
        repository.Add(CreateOrder(Guid.NewGuid(), "other@example.com", "E-20260807-000003", Now));
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new SupportOrderLookupQuery(Email: "ahmed@example.com"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Orders.Count);
        Assert.All(result.Value.Orders, item => Assert.Equal("a***@example.com", item.MaskedEmail));
    }

    [Fact]
    public async Task Handle_Lookup_By_Customer_Returns_Their_Orders()
    {
        var repository = new FakeOrderRepository();
        var customerId = Guid.NewGuid();
        repository.Add(CreateOrder(customerId, "ahmed@example.com", "E-20260807-000001", Now));
        repository.Add(CreateOrder(Guid.NewGuid(), "other@example.com", "E-20260807-000002", Now));
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new SupportOrderLookupQuery(CustomerId: customerId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Orders);
        Assert.Equal("E-20260807-000001", item.OrderNumber);
    }

    [Fact]
    public async Task Handle_No_Filters_Fails_Validation()
    {
        var handler = CreateHandler(new FakeOrderRepository());

        var result = await handler.Handle(
            new SupportOrderLookupQuery(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Handle_Unknown_Order_Number_Returns_Empty()
    {
        var handler = CreateHandler(new FakeOrderRepository());

        var result = await handler.Handle(
            new SupportOrderLookupQuery(OrderNumber: "E-20260807-999999"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Orders);
    }
}
