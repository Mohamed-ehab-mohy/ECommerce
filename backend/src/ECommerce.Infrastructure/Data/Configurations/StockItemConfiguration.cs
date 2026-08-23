using ECommerce.Domain.Inventory;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items", table => table
            .HasCheckConstraint("ck_stock_items_allocated_le_on_hand", "\"allocated\" <= \"on_hand\""));

        builder.HasKey(stockItem => stockItem.Id);
        builder.Property(stockItem => stockItem.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(stockItem => stockItem.Sku)
            .HasMaxLength(50)
            .IsRequired()
            .HasColumnName("sku");
        builder.HasIndex(stockItem => new { stockItem.Sku, stockItem.WarehouseId })
            .IsUnique()
            .HasDatabaseName("ux_stock_items_sku_warehouse");

        builder.Property(stockItem => stockItem.WarehouseId)
            .IsRequired()
            .HasColumnName("warehouse_id");
        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(stockItem => stockItem.WarehouseId)
            .HasConstraintName("fk_stock_items_warehouses");

        builder.Property(stockItem => stockItem.OnHand)
            .IsRequired()
            .HasColumnName("on_hand");

        builder.Property(stockItem => stockItem.Allocated)
            .IsRequired()
            .HasColumnName("allocated");

        builder.Property(stockItem => stockItem.LowStockThreshold)
            .IsRequired()
            .HasDefaultValue(0)
            .HasColumnName("low_stock_threshold");

        builder.Property(stockItem => stockItem.LowStockNotifiedAt)
            .HasColumnName("low_stock_notified_at");

        builder.Property(stockItem => stockItem.LowStockCooldown)
            .IsRequired()
            .HasDefaultValue(TimeSpan.FromHours(24))
            .HasColumnName("low_stock_cooldown");

        builder.Property(stockItem => stockItem.Version)
            .IsRequired()
            .IsConcurrencyToken()
            .HasColumnName("version");

        builder.Property(stockItem => stockItem.CreatedAt).HasColumnName("created_at");
        builder.Property(stockItem => stockItem.UpdatedAt).HasColumnName("updated_at");
        builder.Property(stockItem => stockItem.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(stockItem => stockItem.Available);
        builder.Ignore(stockItem => stockItem.DomainEvents);
    }
}
