using System.Globalization;
using System.Text.RegularExpressions;

namespace ECommerce.Domain.Invoicing;

public sealed record CreditNoteNumber(string Value)
{
    public const int MaxLength = 24;

    private static readonly Regex Pattern = new(
        "^C-\\d{8}-\\d{6}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static CreditNoteNumber Create(DateTime issuedAtUtc, long sequence)
    {
        var datePart = issuedAtUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var sequencePart = (sequence % 1_000_000L).ToString("000000", CultureInfo.InvariantCulture);
        return new CreditNoteNumber($"C-{datePart}-{sequencePart}");
    }

    public static bool TryParse(string? value, out CreditNoteNumber? creditNoteNumber)
    {
        creditNoteNumber = null;

        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            return false;
        }

        if (!Pattern.IsMatch(value))
        {
            return false;
        }

        creditNoteNumber = new CreditNoteNumber(value);
        return true;
    }

    public override string ToString() => Value;
}
