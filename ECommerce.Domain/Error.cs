namespace ECommerce.Domain;

public sealed record Error(string Code, string Message, ErrorType Type);
