using ECommerce.Domain.Events;
using ECommerce.Domain.Invoicing;

namespace ECommerce.UnitTests;

public sealed class InvoiceTests
{
    private static readonly DateTime Now = new(2026, 8, 14, 10, 0, 0, DateTimeKind.Utc);

    private static readonly Guid OrderId = Guid.NewGuid();

    private static readonly IReadOnlyList<InvoiceLine> Lines =
    [
        InvoiceLine.Create(Guid.Empty, "SKU-1", "Widget (SKU-1)", 2, 15.00m, 0.14m, 30.00m)
    ];

    private static Invoice CreateInvoice(decimal total = 100m) =>
        Invoice.Create("I-20260814-000001", OrderId, Guid.NewGuid(), "USD", Lines, 14.00m, 0.14m, total, Now);

    [Fact]
    public void Create_Sets_Issued_Status_Emits_Event_And_Assigns_Lines()
    {
        var invoice = CreateInvoice();

        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
        Assert.Equal("I-20260814-000001", invoice.InvoiceNumber);
        Assert.Equal(OrderId, invoice.OrderId);
        Assert.Equal(Now, invoice.IssuedAt);
        Assert.Equal(0m, invoice.CreditedTotal);

        var line = Assert.Single(invoice.Lines);
        Assert.Equal("SKU-1", line.Sku);
        Assert.Equal(invoice.Id, line.InvoiceId);

        var evt = Assert.Single(invoice.DomainEvents.OfType<InvoiceIssued>());
        Assert.Equal(invoice.Id, evt.InvoiceId);
        Assert.Equal(OrderId, evt.OrderId);
        Assert.Equal(100m, evt.Total);
    }

    [Fact]
    public void ApplyCreditNote_Partial_Advances_To_PartiallyRefunded()
    {
        var invoice = CreateInvoice(100m);

        var result = invoice.ApplyCreditNote(30m, Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(30m, invoice.CreditedTotal);
        Assert.Equal(InvoiceStatus.PartiallyRefunded, invoice.Status);

        var evt = Assert.Single(invoice.DomainEvents.OfType<InvoiceCredited>());
        Assert.Equal(30m, evt.Amount);
        Assert.Equal(70m, evt.RemainingTotal);
    }

    [Fact]
    public void ApplyCreditNote_Full_Advances_To_Refunded()
    {
        var invoice = CreateInvoice(100m);

        var result = invoice.ApplyCreditNote(100m, Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(InvoiceStatus.Refunded, invoice.Status);
        Assert.Equal(100m, invoice.CreditedTotal);
    }

    [Fact]
    public void ApplyCreditNote_Zero_Amount_Is_Rejected()
    {
        var invoice = CreateInvoice(100m);

        var result = invoice.ApplyCreditNote(0m, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(InvoiceErrors.InvalidCreditAmount, result.Error);
    }

    [Fact]
    public void ApplyCreditNote_Exceeding_Remaining_Is_Rejected()
    {
        var invoice = CreateInvoice(100m);

        var result = invoice.ApplyCreditNote(150m, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(InvoiceErrors.CreditExceedsTotal, result.Error);
    }

    [Fact]
    public void ApplyCreditNote_On_Refunded_Invoice_Is_Rejected()
    {
        var invoice = CreateInvoice(100m);
        invoice.ApplyCreditNote(100m, Now);

        var result = invoice.ApplyCreditNote(1m, Now.AddMinutes(1));

        Assert.True(result.IsFailure);
        Assert.Equal(InvoiceErrors.InvoiceNotCreditable, result.Error);
    }

    [Fact]
    public void MarkPaid_Transitions_Issued_Invoice()
    {
        var invoice = CreateInvoice();

        invoice.MarkPaid(Now.AddMinutes(1));

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void MarkPaid_Does_Not_Overwrite_Refunded_Status()
    {
        var invoice = CreateInvoice(100m);
        invoice.ApplyCreditNote(100m, Now);

        invoice.MarkPaid(Now.AddMinutes(1));

        Assert.Equal(InvoiceStatus.Refunded, invoice.Status);
    }

    [Fact]
    public void AttachPdf_Sets_Url()
    {
        var invoice = CreateInvoice();

        invoice.AttachPdf("https://cdn.example.test/invoices/I-20260814-000001.pdf", Now.AddMinutes(1));

        Assert.Equal("https://cdn.example.test/invoices/I-20260814-000001.pdf", invoice.PdfUrl);
    }
}
