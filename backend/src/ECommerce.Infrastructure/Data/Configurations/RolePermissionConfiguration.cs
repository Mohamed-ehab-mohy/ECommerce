using ECommerce.Domain.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(permission => new { permission.RoleId, permission.PermissionCode });

        builder.Property(permission => permission.RoleId).HasColumnName("role_id");
        builder.Property(permission => permission.PermissionCode)
            .HasMaxLength(100)
            .IsRequired()
            .HasColumnName("permission_code");
    }
}
