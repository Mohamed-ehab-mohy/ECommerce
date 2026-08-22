using ECommerce.Domain.Payments;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class RefundItemConfiguration : IEntityTypeConfiguration<RefundItem>
{
    public void Configure(EntityTypeBuilder<RefundItem> builder)
    {
        builder.ToTable("refund_items");

        builder.HasKey(item => new { item.RefundId, item.ProductId });
        builder.Property(item => item.RefundId).HasColumnName("refund_id");
        builder.Property(item => item.ProductId).HasColumnName("product_id");
        builder.Property(item => item.Quantity).HasColumnName("quantity");
    }
}
