using ECommerce.Domain.Identity;
using ECommerce.Shared.Authorization;
using ECommerce.Shared.Primitives;
using ECommerce.UseCases.Common;
using ECommerce.UseCases.Identity.Ports;
using ECommerce.UseCases.Identity.Queries;
using ECommerce.UseCases.Identity.Responses;

namespace ECommerce.UseCases.Identity.Handlers;

public sealed class SearchCustomersQueryHandler(
    IUserRepository users,
    IValidator<SearchCustomersQuery> validator,
    ICurrentUser currentUser) : IRequestHandler<SearchCustomersQuery, Result<PagedCustomersResponse>>
{
    public async Task<Result<PagedCustomersResponse>> Handle(
        SearchCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToResult<PagedCustomersResponse>();
        }

        var includePii = currentUser.Permissions.Contains(Permissions.CustomersPiiRead, StringComparer.Ordinal);

        var items = await users.SearchAsync(request.Email, request.Page, request.PageSize, cancellationToken);
        var total = await users.CountAsync(request.Email, cancellationToken);

        return Result<PagedCustomersResponse>.Success(new PagedCustomersResponse(
            items.Select(customer => CustomerLookupResponseFactory.From(customer, includePii)).ToList(),
            request.Page,
            request.PageSize,
            total));
    }
}

public sealed class GetCustomerQueryHandler(
    IUserRepository users,
    ICurrentUser currentUser) : IRequestHandler<GetCustomerQuery, Result<CustomerLookupResponse>>
{
    public async Task<Result<CustomerLookupResponse>> Handle(
        GetCustomerQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await users.GetByIdAsync(request.CustomerId, cancellationToken);

        if (customer is null)
        {
            return Result<CustomerLookupResponse>.Failure(CustomerErrors.CustomerNotFound);
        }

        var includePii = currentUser.Permissions.Contains(Permissions.CustomersPiiRead, StringComparer.Ordinal);

        return Result<CustomerLookupResponse>.Success(
            CustomerLookupResponseFactory.From(customer, includePii));
    }
}
