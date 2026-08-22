using ECommerce.Domain.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(role => role.Id);
        builder.Property(role => role.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(role => role.Name)
            .HasMaxLength(60)
            .IsRequired()
            .HasColumnName("name");
        builder.HasIndex(role => role.Name)
            .IsUnique()
            .HasDatabaseName("ux_roles_name");

        builder.Property(role => role.Description)
            .HasMaxLength(300)
            .HasColumnName("description");

        builder.HasMany(role => role.Permissions)
            .WithOne()
            .HasForeignKey(permission => permission.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(role => role.CreatedAt).HasColumnName("created_at");
        builder.Property(role => role.UpdatedAt).HasColumnName("updated_at");
        builder.Property(role => role.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(role => role.DomainEvents);
    }
}
