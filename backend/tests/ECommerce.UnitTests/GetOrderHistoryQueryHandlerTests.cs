using ECommerce.Domain.Orders;
using ECommerce.UseCases.Orders.Handlers;
using ECommerce.UseCases.Orders.Queries;

namespace ECommerce.UnitTests;

public sealed class GetOrderHistoryQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 8, 7, 12, 0, 0, DateTimeKind.Utc);

    private static readonly AddressSnapshot Address = new(
        "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");

    private static readonly PriceSnapshot Snapshot = new(
        [new PriceSnapshotItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 2, null)],
        new TotalsSnapshot(30.00m, 10.00m, 0m, 9.90m, 0m, 39.90m, 0m));

    private static Order CreateOrder(Guid customerId, string orderNumber, DateTime placedAt) =>
        Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            customerId,
            "ahmed@example.com",
            "USD",
            orderNumber,
            Snapshot,
            Address,
            Address,
            "standard",
            Guid.NewGuid(),
            placedAt);

    [Fact]
    public async Task Handle_Returns_Only_Requester_Orders_Sorted_Newest_First()
    {
        var customerId = Guid.NewGuid();
        var repository = new FakeOrderRepository();
        repository.Add(CreateOrder(customerId, "E-20260807-000003", Now.AddMinutes(-3)));
        repository.Add(CreateOrder(Guid.NewGuid(), "E-20260807-000009", Now.AddMinutes(-9)));
        repository.Add(CreateOrder(customerId, "E-20260807-000001", Now.AddMinutes(-1)));
        repository.Add(CreateOrder(customerId, "E-20260807-000002", Now.AddMinutes(-2)));
        var handler = new GetOrderHistoryQueryHandler(repository);

        var result = await handler.Handle(
            new GetOrderHistoryQuery(customerId, null, 20),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var numbers = result.Value.Items.Select(item => item.OrderNumber).ToList();
        Assert.Equal(
            ["E-20260807-000001", "E-20260807-000002", "E-20260807-000003"],
            numbers);
    }

    [Fact]
    public async Task Handle_Respects_Page_Size()
    {
        var customerId = Guid.NewGuid();
        var repository = new FakeOrderRepository();
        for (var index = 1; index <= 5; index++)
        {
            repository.Add(CreateOrder(customerId, $"E-20260807-{index:000000}", Now.AddMinutes(-index)));
        }

        var handler = new GetOrderHistoryQueryHandler(repository);

        var result = await handler.Handle(
            new GetOrderHistoryQuery(customerId, null, 2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
    }
}
