using ECommerce.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("warehouses");

        builder.HasKey(warehouse => warehouse.Id);
        builder.Property(warehouse => warehouse.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(warehouse => warehouse.Code)
            .HasMaxLength(32)
            .IsRequired()
            .HasColumnName("code");
        builder.HasIndex(warehouse => warehouse.Code)
            .IsUnique()
            .HasDatabaseName("ux_warehouses_code");

        builder.Property(warehouse => warehouse.Name)
            .HasMaxLength(160)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(warehouse => warehouse.Address)
            .HasMaxLength(500)
            .IsRequired()
            .HasColumnName("address");

        builder.Property(warehouse => warehouse.Timezone)
            .HasMaxLength(64)
            .IsRequired()
            .HasColumnName("timezone");

        builder.Property(warehouse => warehouse.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(warehouse => warehouse.CreatedAt).HasColumnName("created_at");
        builder.Property(warehouse => warehouse.UpdatedAt).HasColumnName("updated_at");
        builder.Property(warehouse => warehouse.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(warehouse => warehouse.DomainEvents);
    }
}
