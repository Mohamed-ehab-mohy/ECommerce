using ECommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(item => new { item.OrderId, item.ProductId });

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

        builder.Property(item => item.Name)
            .HasMaxLength(255)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(item => item.ListPrice)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasColumnName("list_price");

        builder.Property(item => item.UnitPrice)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasColumnName("unit_price");

        builder.Property(item => item.Quantity)
            .IsRequired()
            .HasColumnName("quantity");

        builder.Property(item => item.ImageUrl)
            .HasColumnName("image_url");

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_order_items_quantity_range",
            "\"quantity\" BETWEEN 1 AND 99"));
    }
}
