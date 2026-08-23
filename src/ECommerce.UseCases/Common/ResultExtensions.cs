
namespace ECommerce.UseCases.Common;

public sealed record OperationError(
    int StatusCode,
    string Type,
    string Code,
    string Detail,
    int? RetryAfterSeconds = null,
    string? Permission = null,
    IReadOnlyDictionary<string, object?>? Metadata = null);

public static class ResultExtensions
{
    public static OperationError ToOperationError(this Result result) => result.Error.ToOperationError();

    public static OperationError ToOperationError<T>(this Result<T> result)
        where T : notnull =>
        result.Error.ToOperationError();

    public static OperationError ToOperationError(this Error error)
    {
        var (statusCode, type) = error.Type switch
        {
            ErrorType.Validation => (422, "problems/validation-failed"),
            ErrorType.Conflict => (409, "problems/conflict"),
            ErrorType.NotFound => (404, "problems/not-found"),
            ErrorType.Unauthorized => (401, "problems/unauthorized"),
            ErrorType.Forbidden => (403, "problems/forbidden"),
            ErrorType.Locked => (423, "problems/locked"),
            ErrorType.TooManyRequests => (429, "problems/rate-limited"),
            ErrorType.BadRequest => (400, "problems/bad-request"),
            ErrorType.PaymentRequired => (402, "problems/payment-required"),
            ErrorType.BadGateway => (502, "problems/bad-gateway"),
            _ => (500, "problems/internal")
        };

        return new OperationError(statusCode, type, error.Code, error.Description, error.RetryAfterSeconds, error.Permission, error.Metadata);
    }
}
