
namespace ECommerce.Domain.Identity;

public static class AuthorizationErrors
{
    public static readonly Error NotAuthenticated = new(
        "Authorization.NotAuthenticated",
        "Authentication is required to perform this operation.",
        ErrorType.Unauthorized);

    public static Error PermissionDenied(string permission) => new(
        "Authorization.PermissionDenied",
        $"You do not have the required permission: {permission}.",
        ErrorType.Forbidden,
        Permission: permission);
}
