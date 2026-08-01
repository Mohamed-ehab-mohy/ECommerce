namespace ECommerce.Shared.Errors;

public sealed record Error(string Code, string Description, ErrorType Type = ErrorType.None, int? RetryAfterSeconds = null)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
