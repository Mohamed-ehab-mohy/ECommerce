using ECommerce.Shared.Primitives;

namespace ECommerce.UseCases.Identity.Commands;

public sealed record UpdateProfileCommand(
    Guid CustomerId,
    string? DisplayName,
    string? Phone,
    string? Locale,
    string? Currency) : IRequest<Result>;
