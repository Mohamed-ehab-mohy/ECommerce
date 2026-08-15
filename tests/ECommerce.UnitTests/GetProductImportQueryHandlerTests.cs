using System.Text.Json;
using ECommerce.Domain.Catalog;
using ECommerce.UseCases.Catalog.Handlers;
using ECommerce.UseCases.Catalog.Queries;

namespace ECommerce.UnitTests;

public sealed class GetProductImportQueryHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProductImportRepository _imports = new();

    private GetProductImportQueryHandler CreateHandler() =>
        new(_imports, new GetProductImportQueryValidator());

    [Fact]
    public async Task Handle_Returns_Status_And_Errors()
    {
        var import = ProductImport.Create("[]", 3, UtcNow);
        import.MarkProcessing(UtcNow);
        import.AddSucceeded();
        import.AddSucceeded();
        import.AddError(3, "SKU-9", "unsupported currency", UtcNow);
        import.Complete(UtcNow);
        _imports.Add(import);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetProductImportQuery(import.Id), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal("Completed", result.Value.Status);
        Assert.Equal(3, result.Value.TotalRows);
        Assert.Equal(2, result.Value.SucceededRows);
        Assert.Equal(1, result.Value.FailedRows);
        var error = Assert.Single(result.Value.Errors);
        Assert.Equal(3, error.RowNumber);
        Assert.Equal("SKU-9", error.Sku);
    }

    [Fact]
    public async Task Handle_Missing_Import_Is_Rejected()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetProductImportQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductImportErrors.ImportNotFound, result.Error);
    }
}
