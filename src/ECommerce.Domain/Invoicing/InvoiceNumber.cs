using System.Globalization;
using System.Text.RegularExpressions;

namespace ECommerce.Domain.Invoicing;

public sealed record InvoiceNumber(string Value)
{
    public const int MaxLength = 24;

    private static readonly Regex Pattern = new(
        "^I-\\d{8}-\\d{6}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static InvoiceNumber Create(DateTime issuedAtUtc, long sequence)
    {
        var datePart = issuedAtUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var sequencePart = (sequence % 1_000_000L).ToString("000000", CultureInfo.InvariantCulture);
        return new InvoiceNumber($"I-{datePart}-{sequencePart}");
    }

    public static bool TryParse(string? value, out InvoiceNumber? invoiceNumber)
    {
        invoiceNumber = null;

        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return false;
        }

        if (!Pattern.IsMatch(value))
        {
            return false;
        }

        invoiceNumber = new InvoiceNumber(value);
        return true;
    }

    public override string ToString() => Value;
}
