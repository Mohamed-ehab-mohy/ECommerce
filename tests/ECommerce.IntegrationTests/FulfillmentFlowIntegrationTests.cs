using ECommerce.Domain.Catalog;
using ECommerce.Domain.Fulfillment;
using ECommerce.Domain.Inventory;
using ECommerce.Domain.Orders;
using ECommerce.Infrastructure.Catalog;
using ECommerce.UseCases.Fulfillment.Shipping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ECommerce.Infrastructure.Common;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Fulfillment;
using ECommerce.Infrastructure.Inventory;
using ECommerce.Infrastructure.Orders;
using ECommerce.Infrastructure.Outbox;
using ECommerce.Infrastructure.Shipping;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Fulfillment.Commands;
using ECommerce.UseCases.Fulfillment.Handlers;
using ECommerce.UseCases.Fulfillment.Responses;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using AddressSnapshot = ECommerce.Domain.Orders.AddressSnapshot;

namespace ECommerce.IntegrationTests;

public sealed class FulfillmentFlowIntegrationTests : IClassFixture<PostgresContainerFixture>
{
    private const string Sku = "FUL-01";
    private const string Slug = "fulfillment-widget";
    private const string OrderNumber = "FUL-1001";

    private readonly PostgresContainerFixture _fixture;

    public FulfillmentFlowIntegrationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Fulfillment_Task_Lifecycle_Persists_And_Ships()
    {
        Skip.IfNot(Docker.IsAvailable, "Docker is not available");

        var utcNow = DateTime.UtcNow;
        var (warehouseId, productId, orderId) = await SeedAsync(utcNow);

        var createResult = await CreateTaskAsync(orderId, warehouseId);
        Assert.True(createResult.IsSuccess, createResult.Error.Description);
        var taskId = createResult.Value.TaskId;

        var assignResult = await AssignAsync(taskId, Guid.NewGuid());
        Assert.True(assignResult.IsSuccess, assignResult.Error.Description);

        var pickingResult = await StartPickingAsync(taskId);
        Assert.True(pickingResult.IsSuccess, pickingResult.Error.Description);

        var packedResult = await MarkPackedAsync(taskId);
        Assert.True(packedResult.IsSuccess, packedResult.Error.Description);

        var shipmentResult = await CreateShipmentAsync(taskId);
        Assert.True(shipmentResult.IsSuccess, shipmentResult.Error.Description);
        var shipmentId = shipmentResult.Value.ShipmentId;
        var trackingNumber = shipmentResult.Value.TrackingNumber;

        var inTransitResult = await ApplyTrackingAsync(shipmentId, "InTransit");
        Assert.True(inTransitResult.IsSuccess, inTransitResult.Error.Description);

        var outForDeliveryResult = await ApplyTrackingAsync(shipmentId, "OutForDelivery");
        Assert.True(outForDeliveryResult.IsSuccess, outForDeliveryResult.Error.Description);

        var deliveredResult = await ApplyTrackingAsync(shipmentId, "Delivered");
        Assert.True(deliveredResult.IsSuccess, deliveredResult.Error.Description);

        await using (var verify = CreateContext())
        {
            var task = await verify.FulfillmentTasks
                .Include(candidate => candidate.Items)
                .SingleAsync(candidate => candidate.Id == taskId);
            Assert.Equal(FulfillmentTaskStatus.Shipped, task.Status);
            Assert.NotNull(task.AssignedTo);
            Assert.NotEmpty(task.Items);
            Assert.Contains(task.Items, item => item.Sku == Sku);

            var shipment = await verify.Shipments.SingleAsync(candidate => candidate.Id == shipmentId);
            Assert.Equal("dhl", shipment.CarrierKey);
            Assert.Equal(trackingNumber, shipment.TrackingNumber);
            Assert.Equal(ShipmentStatus.Delivered, shipment.Status);

            var updates = await verify.TrackingUpdates
                .Where(update => update.ShipmentId == shipmentId)
                .ToListAsync();
            Assert.NotEmpty(updates);

            var order = await verify.Orders.SingleAsync(candidate => candidate.Id == orderId);
            Assert.Equal(OrderStatus.Delivered, order.Status);
        }
    }

    private async Task<(Guid WarehouseId, Guid ProductId, Guid OrderId)> SeedAsync(DateTime utcNow)
    {
        await using (var setup = CreateContext())
        {
            await setup.Database.MigrateAsync();
            await setup.Database.ExecuteSqlRawAsync("""
                TRUNCATE TABLE
                    warehouses,
                    stock_items,
                    stock_movements,
                    products,
                    orders,
                    order_items,
                    order_status_log,
                    fulfillment_tasks,
                    fulfillment_task_items,
                    shipments,
                    tracking_updates,
                    outbox_events
                CASCADE;
                """);

            var warehouse = Warehouse.Create("W-FUL", "FUL Warehouse", "Test", "UTC", WarehouseStatus.Active, utcNow);
            setup.Add(warehouse);

            var product = Product.Create(
                Sku, Slug, "en", "Fulfillment Widget", null, "USD", 20.00m, 15.00m,
                null, null, isFeatured: false, ProductStatus.Active, utcNow);
            setup.Add(product);

            var stockItem = StockItem.Create(Sku, warehouse.Id, utcNow);
            stockItem.Apply(
                StockMovement.Create(stockItem.Id, StockMovementType.Receipt, 5, "seed", null, null, utcNow),
                utcNow);
            setup.Add(stockItem);

            var snapshot = new PriceSnapshot(
                [new PriceSnapshotItem(product.Id, Sku, "Fulfillment Widget", 20.00m, 15.00m, 1, null)],
                new TotalsSnapshot(15.00m, 0m, 0m, 9.90m, 0m, 24.90m, 0m));
            var address = new AddressSnapshot(
                "Mona Ali", "0501234567", "1 Marina Walk", "Dubai", "Dubai", "AE", "00000");
            var order = Order.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                "mona@example.com",
                "USD",
                OrderNumber,
                snapshot,
                address,
                address,
                "standard",
                Guid.NewGuid(),
                utcNow);
            order.MarkBackordered([(product.Id, Sku, 1)], utcNow);
            order.FillBackorderItems(Sku, 1, utcNow);
            setup.Add(order);

            await setup.SaveChangesAsync();
            return (warehouse.Id, product.Id, order.Id);
        }
    }

    private async Task<Result<FulfillmentTaskResponse>> CreateTaskAsync(Guid orderId, Guid warehouseId)
    {
        await using var context = CreateContext();
        var handler = new CreateFulfillmentTaskCommandHandler(
            new OrderRepository(context),
            new ProductRepository(context),
            new WarehouseRepository(context),
            new FulfillmentTaskRepository(context),
            new UnitOfWork(context),
            TimeProvider.System,
            new CreateFulfillmentTaskCommandValidator());

        return await handler.Handle(
            new CreateFulfillmentTaskCommand(orderId, warehouseId, Priority: 1, Zone: "A"),
            CancellationToken.None);
    }

    private async Task<Result<FulfillmentTaskResponse>> AssignAsync(Guid taskId, Guid assigneeId)
    {
        await using var context = CreateContext();
        var handler = new AssignFulfillmentTaskCommandHandler(
            new FulfillmentTaskRepository(context),
            new UnitOfWork(context),
            TimeProvider.System,
            new AssignFulfillmentTaskCommandValidator());

        return await handler.Handle(
            new AssignFulfillmentTaskCommand(taskId, assigneeId),
            CancellationToken.None);
    }

    private async Task<Result<FulfillmentTaskResponse>> StartPickingAsync(Guid taskId)
    {
        await using var context = CreateContext();
        var handler = new StartPickingFulfillmentTaskCommandHandler(
            new FulfillmentTaskRepository(context),
            new OrderRepository(context),
            new UnitOfWork(context),
            TimeProvider.System,
            new StartPickingFulfillmentTaskCommandValidator());

        return await handler.Handle(
            new StartPickingFulfillmentTaskCommand(taskId),
            CancellationToken.None);
    }

    private async Task<Result<FulfillmentTaskResponse>> MarkPackedAsync(Guid taskId)
    {
        await using var context = CreateContext();
        var handler = new MarkFulfillmentTaskPackedCommandHandler(
            new FulfillmentTaskRepository(context),
            new OrderRepository(context),
            new UnitOfWork(context),
            TimeProvider.System,
            new MarkFulfillmentTaskPackedCommandValidator());

        return await handler.Handle(
            new MarkFulfillmentTaskPackedCommand(taskId),
            CancellationToken.None);
    }

    private async Task<Result<ShipmentResponse>> CreateShipmentAsync(Guid taskId)
    {
        await using var context = CreateContext();
        var handler = new CreateShipmentCommandHandler(
            new FulfillmentTaskRepository(context),
            new OrderRepository(context),
            new ShipmentRepository(context),
            [new DhlCarrierAdapter(new HttpClient { BaseAddress = new Uri("https://localhost") }, Options.Create(new CarrierOptions()), TimeProvider.System, NullLogger<DhlCarrierAdapter>.Instance), new AramexCarrierAdapter(new HttpClient { BaseAddress = new Uri("https://localhost") }, Options.Create(new CarrierOptions()), TimeProvider.System, NullLogger<AramexCarrierAdapter>.Instance)],
            new UnitOfWork(context),
            TimeProvider.System,
            new CreateShipmentCommandValidator());

        return await handler.Handle(
            new CreateShipmentCommand(taskId, "dhl", "AE", "00000", 250, "USD"),
            CancellationToken.None);
    }

    private async Task<Result<ShipmentResponse>> ApplyTrackingAsync(Guid shipmentId, string status)
    {
        await using var context = CreateContext();
        var handler = new ApplyShipmentTrackingCommandHandler(
            new ShipmentRepository(context),
            new OrderRepository(context),
            new UnitOfWork(context),
            TimeProvider.System,
            new ApplyShipmentTrackingCommandValidator());

        return await handler.Handle(
            new ApplyShipmentTrackingCommand(shipmentId, status),
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
