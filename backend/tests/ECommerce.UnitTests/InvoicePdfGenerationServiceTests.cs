using ECommerce.Domain.Invoicing;
using ECommerce.Domain.Orders;
using ECommerce.Domain.Pricing;
using ECommerce.UseCases.Invoicing.Services;

namespace ECommerce.UnitTests;

public sealed class InvoicePdfGenerationServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 14, 10, 0, 0, DateTimeKind.Utc);

    private readonly FakeInvoiceRepository _invoices = new();

    private readonly FakeOrderRepository _orders = new();

    private readonly FakeInvoicePdfRenderer _renderer = new();

    private readonly FakeInvoiceDocumentStore _documentStore = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private static readonly AddressSnapshot Address = new(
        "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");

    private InvoicePdfGenerationService CreateService() =>
        new(_invoices, _orders, _renderer, _documentStore, _unitOfWork);

    private Order CreateOrder()
    {
        var snapshot = new PriceSnapshot(
            [new PriceSnapshotItem(Guid.NewGuid(), "SKU-1", "Widget", 20.00m, 15.00m, 2, null)],
            new TotalsSnapshot(30.00m, 10.00m, 0m, 9.90m, 0m, 39.90m, 0.14m));

        var order = Order.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ahmed@example.com",
            "USD",
            "E-20260814-000001",
            snapshot,
            Address,
            Address,
            "standard",
            Guid.NewGuid(),
            UtcNow);
        _orders.Add(order);
        return order;
    }

    private Invoice CreateInvoice()
    {
        var order = CreateOrder();
        var invoice = Invoice.Create(
            "I-20260814-000001",
            order.Id,
            order.CustomerId,
            order.Currency,
            [InvoiceLine.Create(Guid.Empty, "SKU-1", "Widget (SKU-1)", 2, 15.00m, 0.14m, 30.00m)],
            order.TaxTotal,
            order.TaxRate,
            order.GrandTotal,
            UtcNow);
        _invoices.Add(invoice);
        return invoice;
    }

    [Fact]
    public async Task Generate_Renders_Stores_And_Attaches_Url()
    {
        var invoice = CreateInvoice();

        var result = await CreateService().GenerateAsync(invoice.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _renderer.RenderCount);
        Assert.Equal("https://cdn.example.test/invoices/I-20260814-000001.pdf", invoice.PdfUrl);

        var document = _renderer.LastDocument!;
        Assert.Equal("I-20260814-000001", document.InvoiceNumber);
        Assert.Equal("E-20260814-000001", document.OrderNumber);
        Assert.Equal("ahmed", document.CustomerName);
        Assert.Equal("ahmed@example.com", document.CustomerEmail);
        Assert.Single(document.Lines);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Generate_When_Pdf_Already_Attached_Skips()
    {
        var invoice = CreateInvoice();
        invoice.AttachPdf("https://cdn.example.test/invoices/I-20260814-000001.pdf", UtcNow);

        var result = await CreateService().GenerateAsync(invoice.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, _renderer.RenderCount);
        Assert.Empty(_documentStore.Documents);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Generate_Unknown_Invoice_Returns_NotFound()
    {
        var result = await CreateService().GenerateAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal(0, _renderer.RenderCount);
    }
}
