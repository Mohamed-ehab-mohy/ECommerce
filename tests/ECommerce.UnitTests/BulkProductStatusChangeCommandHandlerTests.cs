using ECommerce.Domain.Audit;
using ECommerce.Domain.Catalog;
using ECommerce.Shared.Authorization;
using ECommerce.Shared.Errors;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Handlers;

namespace ECommerce.UnitTests;

public sealed class BulkProductStatusChangeCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProductRepository _products = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly FakeAuditLogWriter _audit = new();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private BulkProductStatusChangeCommandHandler CreateHandler() =>
        new(
            _products,
            _unitOfWork,
            new BulkProductStatusChangeCommandValidator(),
            _audit,
            new FixedTimeProvider(UtcNow));

    private static Product CreateProduct(string sku, ProductStatus status) =>
        Product.Create(
            sku,
            sku.ToLowerInvariant(),
            "en",
            "Widget",
            null,
            "USD",
            10m,
            null,
            null,
            null,
            false,
            status,
            UtcNow);

    [Fact]
    public async Task Handle_Activates_And_Deactivates_Products()
    {
        var inactive = CreateProduct("SKU-1", ProductStatus.Inactive);
        var active = CreateProduct("SKU-2", ProductStatus.Active);
        _products.Add(inactive);
        _products.Add(active);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new BulkProductStatusChangeCommand(
            [
                new BulkProductStatusItem(inactive.Id, BulkProductStatusAction.Activate),
                new BulkProductStatusItem(active.Id, BulkProductStatusAction.Deactivate)
            ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(2, result.Value.Processed);
        Assert.Equal(2, result.Value.Succeeded);
        Assert.Equal(0, result.Value.Failed);
        Assert.Equal(ProductStatus.Active, inactive.Status);
        Assert.Equal(ProductStatus.Inactive, active.Status);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_Reports_Missing_Products_As_Failures()
    {
        var product = CreateProduct("SKU-1", ProductStatus.Active);
        _products.Add(product);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new BulkProductStatusChangeCommand(
            [
                new BulkProductStatusItem(product.Id, BulkProductStatusAction.Deactivate),
                new BulkProductStatusItem(Guid.NewGuid(), BulkProductStatusAction.Activate)
            ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(1, result.Value.Succeeded);
        Assert.Equal(1, result.Value.Failed);
        var failedItem = result.Value.Items.Single(item => !item.Success);
        Assert.Contains("was not found", failedItem.Error);
    }

    [Fact]
    public async Task Handle_Noop_When_Already_In_Requested_Status()
    {
        var active = CreateProduct("SKU-1", ProductStatus.Active);
        _products.Add(active);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new BulkProductStatusChangeCommand(
            [
                new BulkProductStatusItem(active.Id, BulkProductStatusAction.Activate)
            ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(0, result.Value.Succeeded);
        Assert.Equal(1, result.Value.Failed);
        Assert.Contains("already in the requested status", result.Value.Items.Single().Error);
        Assert.Equal(ProductStatus.Active, active.Status);
    }

    [Fact]
    public async Task Handle_Empty_Batch_Is_Rejected()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new BulkProductStatusChangeCommand([]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_Writes_Audit_Operation()
    {
        var active = CreateProduct("SKU-1", ProductStatus.Active);
        _products.Add(active);
        var handler = CreateHandler();

        await handler.Handle(
            new BulkProductStatusChangeCommand(
            [
                new BulkProductStatusItem(active.Id, BulkProductStatusAction.Deactivate)
            ]),
            CancellationToken.None);

        var operation = Assert.Single(_audit.Operations);
        Assert.Equal(AuditActions.BulkProductStatusChange, operation.Action);
        Assert.Equal("Product", operation.EntityType);
    }

    [Fact]
    public void Command_Requires_Catalog_Product_Write_Permission()
    {
        var command = new BulkProductStatusChangeCommand(
        [
            new BulkProductStatusItem(Guid.NewGuid(), BulkProductStatusAction.Activate)
        ]);

        Assert.Equal(Permissions.CatalogProductWrite, command.Permission);
    }
}
