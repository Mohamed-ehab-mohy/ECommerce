using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Identity.Responses;

namespace ECommerce.UseCases.Identity.Queries;

public sealed record GetProfileQuery(Guid CustomerId) : IRequest<Result<ProfileResponse>>;
