namespace ECommerce.Shared.Errors;

public enum ErrorType
{
    None,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Internal,
    Locked,
    TooManyRequests,
    BadRequest,
    PaymentRequired,
    BadGateway
}
