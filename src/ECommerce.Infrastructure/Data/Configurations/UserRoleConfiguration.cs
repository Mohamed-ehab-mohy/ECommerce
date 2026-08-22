using ECommerce.Domain.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(userRole => new { userRole.UserId, userRole.RoleId });

        builder.Property(userRole => userRole.UserId).HasColumnName("user_id");
        builder.Property(userRole => userRole.RoleId).HasColumnName("role_id");

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(userRole => userRole.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(userRole => userRole.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(userRole => userRole.RoleId).HasDatabaseName("ix_user_roles_role_id");
    }
}
