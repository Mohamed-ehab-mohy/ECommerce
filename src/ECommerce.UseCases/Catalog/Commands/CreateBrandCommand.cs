
namespace ECommerce.UseCases.Catalog.Commands;

public sealed record CreateBrandCommand(
    string Name,
    string? Description,
    string? Website) : IRequest<Result<Guid>>;
