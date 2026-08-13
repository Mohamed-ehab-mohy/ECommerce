using ECommerce.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class PaymentLedgerConfiguration : IEntityTypeConfiguration<PaymentLedgerEntry>
{
    public void Configure(EntityTypeBuilder<PaymentLedgerEntry> builder)
    {
        builder.ToTable("payment_ledger");

        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).UseIdentityColumn().HasColumnName("id");

        builder.Property(entry => entry.PaymentId).HasColumnName("payment_id");

        builder.Property(entry => entry.Sequence).HasColumnName("sequence");

        builder.Property(entry => entry.EventType)
            .HasMaxLength(40)
            .IsRequired()
            .HasColumnName("event_type");

        builder.Property(entry => entry.Status)
            .HasMaxLength(30)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(entry => entry.Amount)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("amount");

        builder.Property(entry => entry.ProviderReference)
            .HasMaxLength(120)
            .HasColumnName("provider_reference");

        builder.Property(entry => entry.Detail)
            .HasColumnType("text")
            .HasColumnName("detail");

        builder.Property(entry => entry.OccurredAt).HasColumnName("occurred_at");

        builder.HasIndex(entry => entry.PaymentId).HasDatabaseName("ix_payment_ledger_payment_id");
    }
}
