using ECommerce.Domain.Payments;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("refunds");

        builder.HasKey(refund => refund.Id);
        builder.Property(refund => refund.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(refund => refund.OrderId).HasColumnName("order_id");
        builder.Property(refund => refund.PaymentId).HasColumnName("payment_id");

        builder.Property(refund => refund.Amount)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("amount");

        builder.Property(refund => refund.Currency)
            .HasMaxLength(3)
            .IsRequired()
            .HasColumnName("currency");

        builder.Property(refund => refund.Reason)
            .HasMaxLength(500)
            .IsRequired()
            .HasColumnName("reason");

        builder.Property(refund => refund.Restock).HasColumnName("restock");

        builder.Property(refund => refund.IdempotencyKey)
            .HasMaxLength(128)
            .IsRequired()
            .HasColumnName("idempotency_key");

        builder.Property(refund => refund.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(refund => refund.ProviderReference)
            .HasMaxLength(128)
            .HasColumnName("provider_reference");

        builder.Property(refund => refund.FailureDetail)
            .HasMaxLength(1000)
            .HasColumnName("failure_detail");

        builder.Property(refund => refund.ApprovedBy).HasColumnName("approved_by");
        builder.Property(refund => refund.ApprovedAt).HasColumnName("approved_at");
        builder.Property(refund => refund.Attempts).HasColumnName("attempts");

        builder.HasMany(refund => refund.Items)
            .WithOne()
            .HasForeignKey(item => item.RefundId);

        builder.Property(refund => refund.CreatedAt).HasColumnName("created_at");
        builder.Property(refund => refund.UpdatedAt).HasColumnName("updated_at");
        builder.Property(refund => refund.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(refund => refund.DomainEvents);

        builder.HasIndex(refund => refund.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ux_refunds_idempotency_key");

        builder.HasIndex(refund => refund.OrderId).HasDatabaseName("ix_refunds_order_id");
        builder.HasIndex(refund => refund.PaymentId).HasDatabaseName("ix_refunds_payment_id");
        builder.HasIndex(refund => refund.Status).HasDatabaseName("ix_refunds_status");
    }
}
