using ECommerce.Domain.Identity;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Identity.Ports;
using ECommerce.UseCases.Identity.Queries;
using ECommerce.UseCases.Identity.Responses;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class GetProfileQueryHandler(IUserRepository users) : IRequestHandler<GetProfileQuery, Result<ProfileResponse>>
{
    public async Task<Result<ProfileResponse>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var customer = await users.GetByIdAsync(request.CustomerId, cancellationToken);

        return customer is null
            ? Result<ProfileResponse>.Failure(CustomerErrors.CustomerNotFound)
            : Result<ProfileResponse>.Success(new ProfileResponse(
                customer.Id,
                customer.Email,
                customer.DisplayName,
                customer.Phone,
                customer.Locale,
                customer.Currency,
                customer.EmailVerified));
    }
}
