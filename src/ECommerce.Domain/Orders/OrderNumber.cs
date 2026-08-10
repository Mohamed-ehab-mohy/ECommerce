using System.Globalization;
using System.Text.RegularExpressions;

namespace ECommerce.Domain.Orders;

public sealed record OrderNumber(string Value)
{
    public const int MaxLength = 24;

    private static readonly Regex Pattern = new(
        "^E-\\d{8}-\\d{6}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static OrderNumber Create(DateTime placedAtUtc, long sequence)
    {
        var datePart = placedAtUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var sequencePart = (sequence % 1_000_000L).ToString("000000", CultureInfo.InvariantCulture);
        return new OrderNumber($"E-{datePart}-{sequencePart}");
    }

    public static bool TryParse(string? value, out OrderNumber? orderNumber)
    {
        orderNumber = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.Length > MaxLength)
        {
            return false;
        }

        if (!Pattern.IsMatch(value))
        {
            return false;
        }

        orderNumber = new OrderNumber(value);
        return true;
    }

    public override string ToString() => Value;
}
