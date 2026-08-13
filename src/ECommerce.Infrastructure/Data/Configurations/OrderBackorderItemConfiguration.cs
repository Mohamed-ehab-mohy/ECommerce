using ECommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class OrderBackorderItemConfiguration : IEntityTypeConfiguration<OrderBackorderItem>
{
    public void Configure(EntityTypeBuilder<OrderBackorderItem> builder)
    {
        builder.ToTable("order_backorder_items");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(item => item.OrderId)
            .IsRequired()
            .HasColumnName("order_id");

        builder.Property(item => item.ProductId)
            .IsRequired()
            .HasColumnName("product_id");

        builder.Property(item => item.Sku)
            .HasMaxLength(50)
            .IsRequired()
            .HasColumnName("sku");

        builder.Property(item => item.Quantity)
            .IsRequired()
            .HasColumnName("quantity");

        builder.Property(item => item.FilledQuantity)
            .IsRequired()
            .HasColumnName("filled_quantity");

        builder.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(item => item.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");

        builder.Property(item => item.FilledAt)
            .HasColumnName("filled_at");

        builder.HasOne<Order>()
            .WithMany(order => order.BackorderItems)
            .HasForeignKey(item => item.OrderId)
            .HasConstraintName("fk_order_backorder_items_orders");

        builder.HasIndex(item => item.OrderId).HasDatabaseName("ix_order_backorder_items_order_id");
        builder.HasIndex(item => new { item.Sku, item.Status }).HasDatabaseName("ix_order_backorder_items_sku_status");

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_order_backorder_items_quantity_range",
            "\"quantity\" BETWEEN 1 AND 99"));

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_order_backorder_items_filled_range",
            "\"filled_quantity\" BETWEEN 0 AND 99"));
    }
}
