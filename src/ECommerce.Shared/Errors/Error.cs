namespace ECommerce.Shared.Errors;

public sealed record Error(string Code, string Description, ErrorType Type = ErrorType.None)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
