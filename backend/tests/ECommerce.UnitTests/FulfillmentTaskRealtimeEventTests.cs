using ECommerce.Domain.Events;
using ECommerce.Domain.Fulfillment;

namespace ECommerce.UnitTests;

public sealed class FulfillmentTaskRealtimeEventTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 15, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_Raises_FulfillmentTaskCreated()
    {
        var orderId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        var task = FulfillmentTask.Create(orderId, warehouseId, 5, UtcNow, "A");

        var created = Assert.Single(task.DomainEvents.OfType<FulfillmentTaskCreated>());
        Assert.Equal(task.Id, created.TaskId);
        Assert.Equal(orderId, created.OrderId);
        Assert.Equal(warehouseId, created.WarehouseId);
        Assert.Equal("A", created.Zone);
        Assert.Equal(5, created.Priority);
    }

    [Fact]
    public void Split_Raises_FulfillmentTaskCreated_For_New_Part()
    {
        var task = FulfillmentTask.Create(Guid.NewGuid(), Guid.NewGuid(), 5, UtcNow);
        task.AddItem(Guid.NewGuid(), "SKU-1", 2, "A-01");
        task.AddItem(Guid.NewGuid(), "SKU-2", 1, "A-02");
        var movingId = task.Items.First(item => item.Sku == "SKU-2").Id;
        var newWarehouse = Guid.NewGuid();

        var result = task.Split(newWarehouse, [movingId], 3, "B", UtcNow);

        Assert.True(result.IsSuccess, result.Error.Description);
        var created = result.Value.DomainEvents.OfType<FulfillmentTaskCreated>().Single();
        Assert.Equal(result.Value.Id, created.TaskId);
        Assert.Equal(newWarehouse, created.WarehouseId);
        Assert.Equal("B", created.Zone);
        Assert.Equal(3, created.Priority);
    }
}
