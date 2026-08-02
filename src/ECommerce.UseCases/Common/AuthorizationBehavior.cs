using ECommerce.Domain.Identity;
using MediatR;

namespace ECommerce.UseCases.Common;

public sealed class AuthorizationBehavior<TRequest, TResponse>(ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IRequirePermission requiresPermission)
        {
            return await next();
        }

        var hasAccess = currentUser.IsAuthenticated
            && currentUser.Permissions.Contains(requiresPermission.Permission, StringComparer.Ordinal);

        return hasAccess
            ? await next()
            : AuthorizationResultFactory.Forbidden<TResponse>(
                currentUser.IsAuthenticated
                    ? AuthorizationErrors.PermissionDenied(requiresPermission.Permission)
                    : AuthorizationErrors.NotAuthenticated);
    }
}
