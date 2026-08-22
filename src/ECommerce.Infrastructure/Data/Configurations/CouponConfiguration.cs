using ECommerce.Domain.Pricing;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");

        builder.HasKey(coupon => coupon.Id);
        builder.Property(coupon => coupon.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(coupon => coupon.Code)
            .HasMaxLength(64)
            .IsRequired()
            .HasColumnName("code");

        builder.Property(coupon => coupon.PromotionId).HasColumnName("promotion_id");
        builder.Property(coupon => coupon.TotalUses).HasColumnName("total_uses");
        builder.Property(coupon => coupon.UsedCount).HasColumnName("used_count");
        builder.Property(coupon => coupon.PerCustomerLimit).HasColumnName("per_customer_limit");
        builder.Property(coupon => coupon.StartsAt).HasColumnName("starts_at");
        builder.Property(coupon => coupon.EndsAt).HasColumnName("ends_at");

        builder.Property(coupon => coupon.CreatedAt).HasColumnName("created_at");
        builder.Property(coupon => coupon.UpdatedAt).HasColumnName("updated_at");
        builder.Property(coupon => coupon.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(coupon => coupon.DomainEvents);

        builder.HasIndex(coupon => coupon.Code).IsUnique().HasDatabaseName("ux_coupons_code");
        builder.HasIndex(coupon => coupon.PromotionId).HasDatabaseName("ix_coupons_promotion_id");
    }
}

public sealed class CouponUsageConfiguration : IEntityTypeConfiguration<CouponUsage>
{
    public void Configure(EntityTypeBuilder<CouponUsage> builder)
    {
        builder.ToTable("coupon_usages");

        builder.HasKey(usage => usage.Id);
        builder.Property(usage => usage.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(usage => usage.CouponId).HasColumnName("coupon_id");
        builder.Property(usage => usage.OrderId).HasColumnName("order_id");
        builder.Property(usage => usage.CustomerId).HasColumnName("customer_id");
        builder.Property(usage => usage.RedeemedAt).HasColumnName("redeemed_at");

        builder.HasIndex(usage => new { usage.CouponId, usage.OrderId })
            .IsUnique()
            .HasDatabaseName("ux_coupon_usages_coupon_order");

        builder.HasIndex(usage => new { usage.CouponId, usage.CustomerId })
            .HasDatabaseName("ix_coupon_usages_coupon_customer");
    }
}
