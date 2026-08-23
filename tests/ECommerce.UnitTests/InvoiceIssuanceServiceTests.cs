using ECommerce.Domain.Events;
using ECommerce.Domain.Invoicing;
using ECommerce.Domain.Orders;
using ECommerce.Domain.Pricing;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Invoicing.Services;

namespace ECommerce.UnitTests;

public sealed class InvoiceIssuanceServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 14, 10, 0, 0, DateTimeKind.Utc);

    private readonly FakeOrderRepository _orders = new();

    private readonly FakeInvoiceRepository _invoices = new();

    private readonly FakeCreditNoteRepository _creditNotes = new();

    private readonly FakeInvoiceNumberGenerator _invoiceNumbers = new();

    private readonly FakeCreditNoteNumberGenerator _creditNoteNumbers = new();

    private readonly FakeInvoicePdfJobScheduler _pdfJobs = new();

    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly FixedTimeProvider _time = new(UtcNow);

    private static readonly AddressSnapshot Address = new(
        "Ahmed Hassan", "0501234567", "1 Sheikh Zayed Rd", "Dubai", "Dubai", "AE", "00000");

    private InvoiceIssuanceService CreateService() =>
        new(
            _orders,
            _invoices,
            _creditNotes,
            _invoiceNumbers,
            _creditNoteNumbers,
            _pdfJobs,
            _unitOfWork,
            _time);

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

    [Fact]
    public async Task IssueForPaymentCaptured_Creates_Invoice_And_Enqueues_Pdf()
    {
        var order = CreateOrder();
        var paymentCaptured = new PaymentCaptured(Guid.NewGuid(), order.Id, 39.90m, "USD");

        var result = await CreateService().IssueForPaymentCapturedAsync(paymentCaptured, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        var invoice = Assert.Single(_invoices.Invoices);
        Assert.Equal(order.Id, invoice.OrderId);
        Assert.Equal("USD", invoice.Currency);
        Assert.Equal(0.14m, invoice.TaxRate);
        Assert.Equal(39.90m, invoice.Total);
        Assert.Equal(order.Id, invoice.OrderId);
        Assert.StartsWith("I-20260814-", invoice.InvoiceNumber);
        Assert.Single(invoice.Lines);

        var queued = Assert.Single(_pdfJobs.Enqueued);
        Assert.Equal(invoice.Id, queued);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task IssueForPaymentCaptured_When_Invoice_Exists_Returns_Existing_And_Requeues_Pdf()
    {
        var order = CreateOrder();
        var service = CreateService();
        var first = await service.IssueForPaymentCapturedAsync(
            new PaymentCaptured(Guid.NewGuid(), order.Id, 39.90m, "USD"),
            CancellationToken.None);

        var second = await service.IssueForPaymentCapturedAsync(
            new PaymentCaptured(Guid.NewGuid(), order.Id, 39.90m, "USD"),
            CancellationToken.None);

        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.Id, second.Value.Id);
        Assert.Single(_invoices.Invoices);
        Assert.Equal(2, _pdfJobs.Enqueued.Count);
    }

    [Fact]
    public async Task IssueForPaymentCaptured_Missing_Order_Returns_NotFound()
    {
        var result = await CreateService().IssueForPaymentCapturedAsync(
            new PaymentCaptured(Guid.NewGuid(), Guid.NewGuid(), 39.90m, "USD"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Empty(_invoices.Invoices);
    }

    [Fact]
    public async Task IssueForRefund_Creates_CreditNote_And_Credits_Invoice()
    {
        var order = CreateOrder();
        var service = CreateService();
        var invoice = await service.IssueForPaymentCapturedAsync(
            new PaymentCaptured(Guid.NewGuid(), order.Id, 39.90m, "USD"),
            CancellationToken.None);
        var refundId = Guid.NewGuid();

        var result = await service.IssueForRefundAsync(
            new PaymentRefunded(refundId, order.Id, 39.90m, "USD", "ref_mock_1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Description);
        var creditNote = Assert.Single(_creditNotes.CreditNotes);
        Assert.Equal(invoice.Value.Id, creditNote.InvoiceId);
        Assert.Equal(refundId, creditNote.RefundId);
        Assert.Equal(39.90m, creditNote.Amount);
        Assert.Equal("C-20260814-", creditNote.CreditNoteNumber[..11]);

        var stored = _invoices.Invoices.Single();
        Assert.Equal(39.90m, stored.CreditedTotal);
        Assert.Equal(InvoiceStatus.Refunded, stored.Status);
    }

    [Fact]
    public async Task IssueForRefund_When_CreditNote_Exists_Is_Idempotent()
    {
        var order = CreateOrder();
        var service = CreateService();
        await service.IssueForPaymentCapturedAsync(
            new PaymentCaptured(Guid.NewGuid(), order.Id, 39.90m, "USD"),
            CancellationToken.None);
        var paymentRefunded = new PaymentRefunded(Guid.NewGuid(), order.Id, 39.90m, "USD", "ref_mock_1");

        var first = await service.IssueForRefundAsync(paymentRefunded, CancellationToken.None);
        var second = await service.IssueForRefundAsync(paymentRefunded, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.Id, second.Value.Id);
        Assert.Single(_creditNotes.CreditNotes);
    }

    [Fact]
    public async Task IssueForRefund_Without_Invoice_Returns_NotFound()
    {
        var result = await CreateService().IssueForRefundAsync(
            new PaymentRefunded(Guid.NewGuid(), Guid.NewGuid(), 39.90m, "USD", "ref_mock_1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Empty(_creditNotes.CreditNotes);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
