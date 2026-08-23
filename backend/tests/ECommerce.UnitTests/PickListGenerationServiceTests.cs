using ECommerce.Domain.Fulfillment;
using ECommerce.UseCases.Fulfillment.Services;

namespace ECommerce.UnitTests;

public sealed class PickListGenerationServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 13, 16, 0, 0, DateTimeKind.Utc);

    private static readonly Guid WarehouseId = Guid.NewGuid();

    private static FulfillmentTask CreateTask(Guid orderId, string? zone, int priority = 1)
    {
        var task = FulfillmentTask.Create(orderId, WarehouseId, priority, UtcNow, zone);
        return task;
    }

    private static FulfillmentTask CreateTaskWithItems(Guid orderId, string? zone, params (string Sku, string? Bin, int Qty)[] items)
    {
        var task = CreateTask(orderId, zone);
        foreach (var (sku, bin, qty) in items)
        {
            task.AddItem(Guid.NewGuid(), sku, qty, bin);
        }

        return task;
    }

    [Fact]
    public void Generate_Groups_By_Zone_And_Orders_By_Bin()
    {
        var orderA = Guid.NewGuid();
        var orderB = Guid.NewGuid();
        var orderNumbers = new Dictionary<Guid, string>
        {
            [orderA] = "E-1",
            [orderB] = "E-2"
        };

        var tasks = new List<FulfillmentTask>
        {
            CreateTaskWithItems(orderA, "A", ("SKU-1", "A-05", 2), ("SKU-2", "A-01", 1)),
            CreateTaskWithItems(orderB, "A", ("SKU-3", null, 4)),
            CreateTaskWithItems(orderB, "B", ("SKU-4", "B-02", 3))
        };

        var lists = new PickListGenerationService().Generate("DXB", tasks, orderNumbers);

        Assert.Equal(2, lists.Count);
        Assert.Equal("A", lists[0].Zone);
        Assert.Equal("B", lists[1].Zone);
        Assert.Equal("DXB", lists[0].WarehouseCode);

        var zoneA = lists[0].Lines;
        Assert.Equal(3, zoneA.Count);
        Assert.Equal("A-01", zoneA[0].BinLocation);
        Assert.Equal("SKU-2", zoneA[0].Sku);
        Assert.Null(zoneA[2].BinLocation);
        Assert.Equal(7, lists[0].TotalItems);
        Assert.Equal("E-1", zoneA[0].OrderNumber);
    }

    [Fact]
    public void Generate_Chunks_Zone_At_Max_Lines()
    {
        var orderNumbers = new Dictionary<Guid, string> { [Guid.NewGuid()] = "E-1" };
        var tasks = new List<FulfillmentTask>();

        for (var i = 0; i < 30; i++)
        {
            var task = CreateTask(Guid.NewGuid(), "A");
            task.AddItem(Guid.NewGuid(), $"SKU-{i}", 1, $"A-{i:D2}");
            tasks.Add(task);
            orderNumbers[task.OrderId] = $"E-{i + 1}";
        }

        var lists = new PickListGenerationService().Generate("DXB", tasks, orderNumbers, maxLinesPerList: 25);

        Assert.Equal(2, lists.Count);
        Assert.Equal(25, lists[0].Lines.Count);
        Assert.Equal(5, lists[1].Lines.Count);
    }

    [Fact]
    public void Generate_Skips_Tasks_Without_Order_Numbers()
    {
        var task = CreateTaskWithItems(Guid.NewGuid(), "A", ("SKU-1", "A-01", 2));

        var lists = new PickListGenerationService().Generate("DXB", [task], new Dictionary<Guid, string>());

        Assert.Empty(lists);
    }

    [Fact]
    public void Generate_Returns_Empty_For_No_Tasks()
    {
        var lists = new PickListGenerationService().Generate("DXB", [], new Dictionary<Guid, string>());

        Assert.Empty(lists);
    }

    [Fact]
    public void Generate_Defaults_Unzoned_Tasks_To_Unzoned_Group()
    {
        var order = Guid.NewGuid();
        var task = CreateTaskWithItems(order, null, ("SKU-1", null, 2));
        var orderNumbers = new Dictionary<Guid, string> { [order] = "E-1" };

        var lists = new PickListGenerationService().Generate("DXB", [task], orderNumbers);

        var list = Assert.Single(lists);
        Assert.Equal("UNZONED", list.Zone);
    }
}
