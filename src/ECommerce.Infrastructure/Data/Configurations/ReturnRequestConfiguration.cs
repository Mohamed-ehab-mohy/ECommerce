using ECommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.ToTable("return_requests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.OrderId).HasColumnName("order_id");
        builder.Property(r => r.CustomerId).HasColumnName("customer_id");
        builder.Property(r => r.Reason).HasColumnName("reason").HasMaxLength(1000);
        builder.Property(r => r.Currency).HasColumnName("currency").HasMaxLength(3);
        builder.Property(r => r.RefundAmount).HasColumnName("refund_amount").HasPrecision(18, 2);
        builder.Property(r => r.Restock).HasColumnName("restock");
        builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(50);
        builder.Property(r => r.AdminNotes).HasColumnName("admin_notes").HasMaxLength(2000);
        builder.Property(r => r.ReviewedBy).HasColumnName("reviewed_by");
        builder.Property(r => r.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");
    }
}

public sealed class ReturnRequestItemConfiguration : IEntityTypeConfiguration<ReturnRequestItem>
{
    public void Configure(EntityTypeBuilder<ReturnRequestItem> builder)
    {
        builder.ToTable("return_request_items");

        builder.HasKey(item => new { item.ReturnRequestId, item.ProductId });
        builder.Property(item => item.ReturnRequestId).HasColumnName("return_request_id");
        builder.Property(item => item.OrderItemId).HasColumnName("order_item_id");
        builder.Property(item => item.ProductId).HasColumnName("product_id");
        builder.Property(item => item.Sku).HasColumnName("sku").HasMaxLength(100);
        builder.Property(item => item.Quantity).HasColumnName("quantity");
        builder.Property(item => item.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
        builder.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(500);
    }
}
