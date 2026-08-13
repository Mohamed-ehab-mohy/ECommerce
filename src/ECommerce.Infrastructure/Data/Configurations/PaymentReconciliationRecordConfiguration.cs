using ECommerce.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class PaymentReconciliationRecordConfiguration : IEntityTypeConfiguration<PaymentReconciliationRecord>
{
    public void Configure(EntityTypeBuilder<PaymentReconciliationRecord> builder)
    {
        builder.ToTable("payment_reconciliation_records");

        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(record => record.PaymentId).HasColumnName("payment_id");

        builder.Property(record => record.ProviderKey)
            .HasMaxLength(30)
            .IsRequired()
            .HasColumnName("provider_key");

        builder.Property(record => record.ProviderReference)
            .HasMaxLength(120)
            .IsRequired()
            .HasColumnName("provider_reference");

        builder.Property(record => record.Amount)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("amount");

        builder.Property(record => record.Currency)
            .HasMaxLength(3)
            .IsRequired()
            .HasColumnName("currency");

        builder.Property(record => record.RecordedStatus)
            .HasMaxLength(30)
            .IsRequired()
            .HasColumnName("recorded_status");

        builder.Property(record => record.ProviderStatus)
            .HasMaxLength(30)
            .IsRequired()
            .HasColumnName("provider_status");

        builder.Property(record => record.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(record => record.Detail)
            .HasColumnType("text")
            .IsRequired()
            .HasColumnName("detail");

        builder.Property(record => record.CheckedAtUtc).HasColumnName("checked_at_utc");

        builder.Property(record => record.CreatedAt).HasColumnName("created_at");
        builder.Property(record => record.UpdatedAt).HasColumnName("updated_at");
        builder.Property(record => record.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(record => record.DomainEvents);

        builder.HasIndex(record => record.PaymentId).HasDatabaseName("ux_reconciliation_payment_id").IsUnique();
        builder.HasIndex(record => record.Status).HasDatabaseName("ix_reconciliation_status");
    }
}
