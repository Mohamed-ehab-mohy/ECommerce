namespace ECommerce.UseCases.Common;

public static class PiiMasker
{
    public static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        return at <= 0 ? email : $"{email[..1]}***{email[at..]}";
    }

    public static string? MaskPhone(string? phone) =>
        string.IsNullOrWhiteSpace(phone)
            ? phone
            : phone.Length <= 4
                ? new string('*', phone.Length)
                : new string('*', phone.Length - 4) + phone[^4..];
}
