using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Identity.Responses;
using MediatR;

namespace ECommerce.UseCases.Identity.Queries;

public sealed record GetProfileQuery(Guid CustomerId) : IRequest<Result<ProfileResponse>>;
