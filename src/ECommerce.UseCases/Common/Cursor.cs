using System.Globalization;
using System.Text;

namespace ECommerce.UseCases.Common;

public static class Cursor
{
    public static string Encode(DateTime at, Guid id)
    {
        var payload = $"{at:O}|{id:N}";
        var bytes = Encoding.UTF8.GetBytes(payload);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool TryDecode(string? cursor, out DateTime at, out Guid id)
    {
        at = default;
        id = default;

        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }

        try
        {
            var normalized = cursor.Replace('-', '+').Replace('_', '/');
            normalized = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(normalized)).Split('|');
            return parts.Length == 2
                && DateTime.TryParse(
                    parts[0],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal,
                    out at)
                && Guid.TryParseExact(parts[1], "N", out id);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
