using ECommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(order => order.CheckoutId).HasColumnName("checkout_id");
        builder.Property(order => order.CartId).HasColumnName("cart_id");
        builder.Property(order => order.CustomerId).HasColumnName("customer_id");

        builder.Property(order => order.OrderNumber)
            .HasMaxLength(24)
            .IsRequired()
            .HasColumnName("order_number");

        builder.Property(order => order.CustomerEmail)
            .HasMaxLength(254)
            .IsRequired()
            .HasColumnName("customer_email");

        builder.Property(order => order.Currency)
            .HasMaxLength(3)
            .IsRequired()
            .HasColumnName("currency");

        builder.Property(order => order.Subtotal)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("subtotal");

        builder.Property(order => order.ItemDiscount)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("item_discount");

        builder.Property(order => order.CartDiscount)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("cart_discount");

        builder.Property(order => order.ShippingTotal)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("shipping_total");

        builder.Property(order => order.TaxTotal)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("tax_total");

        builder.Property(order => order.GrandTotal)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("grand_total");

        builder.Property(order => order.CouponId).HasColumnName("coupon_id");

        builder.Property(order => order.AppliedPromotionIds)
            .HasColumnType("jsonb")
            .HasColumnName("applied_promotion_ids")
            .HasConversion(new JsonValueConverter<IReadOnlyList<Guid>>())
            .IsRequired();

        builder.Property(order => order.ShippingAddress)
            .HasColumnType("jsonb")
            .HasColumnName("shipping_address")
            .IsRequired();

        builder.Property(order => order.BillingAddress)
            .HasColumnType("jsonb")
            .HasColumnName("billing_address")
            .IsRequired();

        builder.Property(order => order.ShippingMethodId)
            .HasMaxLength(64)
            .IsRequired()
            .HasColumnName("shipping_method_id");

        builder.Property(order => order.PaymentId).HasColumnName("payment_id");

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(order => order.PlacedAt).HasColumnName("placed_at");

        builder.Property(order => order.CreatedAt).HasColumnName("created_at");
        builder.Property(order => order.UpdatedAt).HasColumnName("updated_at");
        builder.Property(order => order.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(order => order.DomainEvents);

        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey(item => item.OrderId)
            .HasConstraintName("fk_order_items_orders");

        builder.HasMany(order => order.StatusLogs)
            .WithOne()
            .HasForeignKey(entry => entry.OrderId)
            .HasConstraintName("fk_order_status_log_orders");

        builder.HasIndex(order => order.OrderNumber).IsUnique().HasDatabaseName("ux_orders_order_number");
        builder.HasIndex(order => order.CheckoutId).HasDatabaseName("ix_orders_checkout_id");
        builder.HasIndex(order => order.PaymentId).HasDatabaseName("ix_orders_payment_id");
        builder.HasIndex(order => order.CartId).HasDatabaseName("ix_orders_cart_id");
        builder.HasIndex(order => new { order.CustomerId, order.PlacedAt }).HasDatabaseName("ix_orders_customer_id_placed_at");
        builder.HasIndex(order => order.PlacedAt).HasDatabaseName("ix_orders_placed_at");
        builder.HasIndex(order => order.Status).HasDatabaseName("ix_orders_status");
    }
}
