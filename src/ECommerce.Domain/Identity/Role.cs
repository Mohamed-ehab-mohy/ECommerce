using ECommerce.Domain.Common;
using ECommerce.Shared.Primitives;

namespace ECommerce.Domain.Identity;

public sealed class Role : BaseEntity<Guid>
{
    private readonly List<RolePermission> _permissions = [];

    private Role()
    {
        Name = string.Empty;
        Description = string.Empty;
    }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions;

    public static Role Create(string name, string? description, DateTime utcNow)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description?.Trim() ?? string.Empty,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    public void Update(string name, string? description, DateTime utcNow)
    {
        Name = name;
        Description = description?.Trim() ?? string.Empty;
        UpdatedAt = utcNow;
    }

    public void AssignPermissions(IEnumerable<string> permissionCodes, DateTime utcNow)
    {
        _permissions.Clear();
        _permissions.AddRange(permissionCodes
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .Select(code => RolePermission.Create(Id, code)));

        UpdatedAt = utcNow;
    }

    public bool HasPermission(string permissionCode) =>
        _permissions.Any(permission => permission.PermissionCode == permissionCode);
}
