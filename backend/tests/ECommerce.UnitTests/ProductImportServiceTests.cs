using System.Text.Json;
using ECommerce.Domain.Catalog;
using ECommerce.UseCases.Catalog.Commands;
using ECommerce.UseCases.Catalog.Services;
using ECommerce.UseCases.Pricing;
using Microsoft.Extensions.Logging.Abstractions;

namespace ECommerce.UnitTests;

public sealed class ProductImportServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 14, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeProductImportRepository _imports = new();

    private readonly FakeProductRepository _products = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly FakeAuditLogWriter _audit = new();

    private readonly ProductImportService _service;

    public ProductImportServiceTests()
    {
        _service = new ProductImportService(
            _imports,
            _products,
            _unitOfWork,
            new FixedTimeProvider(UtcNow),
            new DefaultCurrencyCatalog(),
            new DefaultLocaleCatalog(),
            _audit,
            NullLogger<ProductImportService>.Instance);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static ProductImportRow Row(
        string sku,
        string name = "Widget",
        decimal list = 15.00m,
        string currency = "USD",
        string? slug = null) =>
        new(sku, name, currency, list, null, slug, null, null, null, false, null, "en");

    private static ProductImport CreateImport(params ProductImportRow[] rows)
    {
        var json = JsonSerializer.Serialize(rows);
        return ProductImport.Create(json, rows.Length, UtcNow);
    }

    [Fact]
    public async Task Process_Creates_Products_For_Valid_Rows()
    {
        var import = CreateImport(Row("SKU-1"), Row("SKU-2", name: "Gadget"));
        _imports.Add(import);

        await _service.ProcessAsync(import.Id, CancellationToken.None);

        Assert.Equal(ProductImportStatus.Completed, import.Status);
        Assert.Equal(2, import.SucceededRows);
        Assert.Equal(0, import.FailedRows);
        Assert.Equal(2, _products.Products.Count);
        Assert.Equal("SKU-1", _products.Products[0].Sku);
    }

    [Fact]
    public async Task Process_Reports_Per_Row_Errors_And_Succeeds_Others()
    {
        var import = CreateImport(
            Row("SKU-1"),
            Row("SKU-2", currency: "XXX"),
            Row("SKU-3"));
        _imports.Add(import);

        await _service.ProcessAsync(import.Id, CancellationToken.None);

        Assert.Equal(ProductImportStatus.Completed, import.Status);
        Assert.Equal(2, import.SucceededRows);
        Assert.Equal(1, import.FailedRows);
        Assert.Equal(2, _products.Products.Count);
        var error = Assert.Single(import.Errors);
        Assert.Equal(2, error.RowNumber);
        Assert.Equal("SKU-2", error.Sku);
        Assert.Contains("not supported", error.Message);
    }

    [Fact]
    public async Task Process_Rejects_Duplicate_Sku_Within_Batch()
    {
        var import = CreateImport(Row("SKU-1"), Row("sku-1"));
        _imports.Add(import);

        await _service.ProcessAsync(import.Id, CancellationToken.None);

        Assert.Equal(1, import.SucceededRows);
        Assert.Equal(1, import.FailedRows);
        Assert.Single(import.Errors);
        Assert.Contains("Duplicate SKU", import.Errors.First().Message);
    }

    [Fact]
    public async Task Process_Rejects_Sku_That_Already_Exists()
    {
        _products.Add(Product.Create(
            "SKU-1", "existing", "en", "Existing", null, "USD", 10m, null, null, null, false,
            ProductStatus.Active, UtcNow));
        var import = CreateImport(Row("SKU-1"));
        _imports.Add(import);

        await _service.ProcessAsync(import.Id, CancellationToken.None);

        Assert.Equal(0, import.SucceededRows);
        Assert.Equal(1, import.FailedRows);
        Assert.Contains("already exists", import.Errors.First().Message);
    }

    [Fact]
    public async Task Process_Rejects_Invalid_Row_Data()
    {
        var import = CreateImport(
            new ProductImportRow("AB", "Widget", "USD", -5m, null, null, null, null, null, false, null, "en"));
        _imports.Add(import);

        await _service.ProcessAsync(import.Id, CancellationToken.None);

        Assert.Equal(1, import.FailedRows);
        Assert.Single(import.Errors);
        Assert.Equal(1, import.Errors.First().RowNumber);
    }

    [Fact]
    public async Task Process_Generates_Slug_From_Name_When_Missing()
    {
        var import = CreateImport(Row("SKU-1", name: "Hello World Widget"));
        _imports.Add(import);

        await _service.ProcessAsync(import.Id, CancellationToken.None);

        var product = Assert.Single(_products.Products);
        Assert.Equal("hello-world-widget", product.Slug);
    }

    [Fact]
    public async Task Process_Skips_Already_Completed_Import()
    {
        var import = CreateImport(Row("SKU-1"));
        import.Complete(UtcNow);
        _imports.Add(import);

        await _service.ProcessAsync(import.Id, CancellationToken.None);

        Assert.Empty(_products.Products);
    }

    [Fact]
    public async Task Process_Invalid_Json_Fails_Import()
    {
        var import = ProductImport.Create("{not valid json", 1, UtcNow);
        _imports.Add(import);

        await _service.ProcessAsync(import.Id, CancellationToken.None);

        Assert.Equal(ProductImportStatus.Failed, import.Status);
        Assert.Empty(_products.Products);
    }

    [Fact]
    public async Task Process_Writes_Import_Audit()
    {
        var import = CreateImport(Row("SKU-1"));
        _imports.Add(import);

        await _service.ProcessAsync(import.Id, CancellationToken.None);

        var operation = Assert.Single(_audit.Operations);
        Assert.Equal("catalog.import.run", operation.Action);
        Assert.Equal(import.Id.ToString(), operation.EntityId);
    }
}
