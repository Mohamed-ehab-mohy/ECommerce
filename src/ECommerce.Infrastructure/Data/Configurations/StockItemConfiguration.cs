using ECommerce.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items");

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

        builder.Property(stockItem => stockItem.CreatedAt).HasColumnName("created_at");
        builder.Property(stockItem => stockItem.UpdatedAt).HasColumnName("updated_at");
        builder.Property(stockItem => stockItem.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(stockItem => stockItem.Available);
        builder.Ignore(stockItem => stockItem.DomainEvents);
    }
}
