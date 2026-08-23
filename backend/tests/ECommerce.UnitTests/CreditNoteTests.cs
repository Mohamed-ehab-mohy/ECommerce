using ECommerce.Domain.Events;
using ECommerce.Domain.Invoicing;

namespace ECommerce.UnitTests;

public sealed class CreditNoteTests
{
    private static readonly DateTime Now = new(2026, 8, 14, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_Sets_Fields_And_Emits_Event()
    {
        var invoiceId = Guid.NewGuid();
        var refundId = Guid.NewGuid();

        var creditNote = CreditNote.Create(
            "C-20260814-000001",
            invoiceId,
            refundId,
            39.90m,
            "Refund completed",
            "USD",
            Now);

        Assert.Equal("C-20260814-000001", creditNote.CreditNoteNumber);
        Assert.Equal(invoiceId, creditNote.InvoiceId);
        Assert.Equal(refundId, creditNote.RefundId);
        Assert.Equal(39.90m, creditNote.Amount);
        Assert.Equal(Now, creditNote.IssuedAt);

        var evt = Assert.Single(creditNote.DomainEvents.OfType<CreditNoteIssued>());
        Assert.Equal(creditNote.Id, evt.CreditNoteId);
        Assert.Equal(invoiceId, evt.InvoiceId);
        Assert.Equal(refundId, evt.RefundId);
        Assert.Equal(39.90m, evt.Amount);
    }

    [Fact]
    public void InvoiceNumber_Create_And_Parse_Roundtrip()
    {
        var number = InvoiceNumber.Create(Now, 42);

        Assert.Equal("I-20260814-000042", number.Value);
        Assert.True(InvoiceNumber.TryParse(number.Value, out var parsed));
        Assert.Equal(number, parsed);
    }

    [Fact]
    public void InvoiceNumber_TryParse_Rejects_Malformed()
    {
        Assert.False(InvoiceNumber.TryParse("E-20260814-000042", out _));
        Assert.False(InvoiceNumber.TryParse("I-20260814", out _));
        Assert.False(InvoiceNumber.TryParse("I-20260814-0000A2", out _));
    }

    [Fact]
    public void CreditNoteNumber_Create_And_Parse_Roundtrip()
    {
        var number = CreditNoteNumber.Create(Now, 7);

        Assert.Equal("C-20260814-000007", number.Value);
        Assert.True(CreditNoteNumber.TryParse(number.Value, out var parsed));
        Assert.Equal(number, parsed);
    }

    [Fact]
    public void CreditNoteNumber_TryParse_Rejects_Malformed()
    {
        Assert.False(CreditNoteNumber.TryParse("I-20260814-000007", out _));
        Assert.False(CreditNoteNumber.TryParse("C-20260814-000007-extra", out _));
    }
}
