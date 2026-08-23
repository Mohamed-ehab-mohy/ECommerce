
namespace ECommerce.Domain.Identity;

public static class RoleErrors
{
    public static readonly Error RoleNotFound = new(
        "Role.NotFound",
        "The role was not found.",
        ErrorType.NotFound);

    public static readonly Error NameAlreadyExists = new(
        "Role.NameAlreadyExists",
        "A role with this name already exists.",
        ErrorType.Conflict);

    public static readonly Error PermissionNotRegistered = new(
        "Role.PermissionNotRegistered",
        "One or more permission codes are not part of the registered permission catalog.",
        ErrorType.Validation);
}
