using ECommerce.Domain.Catalog;
using ECommerce.Shared.Authorization;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Handlers;

namespace ECommerce.UnitTests;

public sealed class StartProductImportCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProductImportRepository _imports = new();

    private readonly FakeProductImportJobScheduler _scheduler = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private StartProductImportCommandHandler CreateHandler() =>
        new(
            _imports,
            _scheduler,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            new StartProductImportCommandValidator());

    private static ProductImportRow Row(string sku = "SKU-1") =>
        new(sku, "Widget", "USD", 15.00m, null, null, null, null, null, false, null, "en");

    [Fact]
    public async Task Handle_Creates_Import_And_Enqueues_Job()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new StartProductImportCommand([Row(), Row("SKU-2")]),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        var import = Assert.Single(_imports.Imports);
        Assert.Equal(result.Value.ImportId, import.Id);
        Assert.Equal(ProductImportStatus.Queued, import.Status);
        Assert.Equal(2, import.TotalRows);
        Assert.Equal(import.Id, Assert.Single(_scheduler.Enqueued));
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_Empty_Batch_Is_Rejected()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new StartProductImportCommand([]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_imports.Imports);
        Assert.Empty(_scheduler.Enqueued);
    }

    [Fact]
    public async Task Handle_Overly_Large_Batch_Is_Rejected()
    {
        var handler = CreateHandler();
        var rows = Enumerable.Range(1, StartProductImportCommandValidator.MaxBatchSize + 1)
            .Select(index => Row($"SKU-{index}"))
            .ToList();

        var result = await handler.Handle(
            new StartProductImportCommand(rows),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_imports.Imports);
    }

    [Fact]
    public void Command_Requires_Catalog_Product_Write_Permission()
    {
        var command = new StartProductImportCommand([Row()]);

        Assert.Equal(Permissions.CatalogProductWrite, command.Permission);
    }
}
