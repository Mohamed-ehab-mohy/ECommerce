namespace ECommerce.Domain.Identity;

public sealed class RolePermission
{
    private RolePermission()
    {
        PermissionCode = string.Empty;
    }

    public Guid RoleId { get; private set; }

    public string PermissionCode { get; private set; }

    public static RolePermission Create(Guid roleId, string permissionCode) =>
        new() { RoleId = roleId, PermissionCode = permissionCode };
}
