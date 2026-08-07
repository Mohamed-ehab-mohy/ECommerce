namespace ECommerce.Shared.Errors;

public sealed record Error(
    string Code,
    string Description,
    ErrorType Type = ErrorType.None,
    int? RetryAfterSeconds = null,
    string? Permission = null)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }

    public Error With(IReadOnlyDictionary<string, object?> metadata) => this with { Metadata = metadata };
}
