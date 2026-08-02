using ECommerce.Shared.Primitives;
using MediatR;

namespace ECommerce.UseCases.Catalog.Commands;

public sealed record DeactivateProductCommand(Guid ProductId) : IRequest<Result>;
