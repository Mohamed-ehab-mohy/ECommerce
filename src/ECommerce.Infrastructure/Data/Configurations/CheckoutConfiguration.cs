using ECommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class CheckoutConfiguration : IEntityTypeConfiguration<Checkout>
{
    public void Configure(EntityTypeBuilder<Checkout> builder)
    {
        builder.ToTable("checkouts");

        builder.HasKey(checkout => checkout.Id);
        builder.Property(checkout => checkout.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(checkout => checkout.CartId).HasColumnName("cart_id");
        builder.Property(checkout => checkout.CustomerId).HasColumnName("customer_id");

        builder.Property(checkout => checkout.CustomerEmail)
            .HasMaxLength(254)
            .IsRequired()
            .HasColumnName("customer_email");

        builder.Property(checkout => checkout.Currency)
            .HasMaxLength(3)
            .IsRequired()
            .HasColumnName("currency");

        builder.Property(checkout => checkout.PriceSnapshot)
            .HasColumnType("jsonb")
            .HasColumnName("price_snapshot")
            .IsRequired();

        builder.Property(checkout => checkout.AppliedCouponId).HasColumnName("applied_coupon_id");

        builder.Property(checkout => checkout.AppliedPromotionIds)
            .HasColumnType("jsonb")
            .HasColumnName("applied_promotion_ids")
            .HasConversion(new JsonValueConverter<IReadOnlyList<Guid>>())
            .IsRequired();

        builder.Property(checkout => checkout.ShippingAddress)
            .HasColumnType("jsonb")
            .HasColumnName("shipping_address")
            .IsRequired();

        builder.Property(checkout => checkout.BillingAddress)
            .HasColumnType("jsonb")
            .HasColumnName("billing_address")
            .IsRequired();

        builder.Property(checkout => checkout.ShippingMethodId)
            .HasMaxLength(64)
            .IsRequired()
            .HasColumnName("shipping_method_id");

        builder.Property(checkout => checkout.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(checkout => checkout.ExpiresAt).HasColumnName("expires_at");
        builder.Property(checkout => checkout.PaymentId).HasColumnName("payment_id");
        builder.Property(checkout => checkout.PlacedAt).HasColumnName("placed_at");

        builder.Property(checkout => checkout.CreatedAt).HasColumnName("created_at");
        builder.Property(checkout => checkout.UpdatedAt).HasColumnName("updated_at");
        builder.Property(checkout => checkout.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(checkout => checkout.DomainEvents);

        builder.HasIndex(checkout => checkout.CartId).HasDatabaseName("ix_checkouts_cart_id");
        builder.HasIndex(checkout => checkout.PaymentId).HasDatabaseName("ix_checkouts_payment_id");
    }
}
