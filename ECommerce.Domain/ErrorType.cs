namespace ECommerce.Domain;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    InternalServerError
}
