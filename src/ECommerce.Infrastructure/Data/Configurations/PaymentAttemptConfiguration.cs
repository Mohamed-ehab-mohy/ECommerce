using ECommerce.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.ToTable("payment_attempts");

        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.Id).ValueGeneratedOnAdd().HasColumnName("id");

        builder.Property(attempt => attempt.PaymentId).IsRequired().HasColumnName("payment_id");

        builder.Property(attempt => attempt.AttemptNo).IsRequired().HasColumnName("attempt_no");

        builder.Property(attempt => attempt.Action)
            .HasMaxLength(20)
            .IsRequired()
            .HasColumnName("action");

        builder.Property(attempt => attempt.Amount)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("amount");

        builder.Property(attempt => attempt.ProviderResponse)
            .HasColumnType("jsonb")
            .HasColumnName("provider_response");

        builder.Property(attempt => attempt.Status)
            .HasMaxLength(20)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(attempt => attempt.TraceId)
            .HasMaxLength(32)
            .HasColumnName("trace_id");

        builder.Property(attempt => attempt.OccurredAt).IsRequired().HasColumnName("occurred_at");

        builder.HasIndex(attempt => attempt.PaymentId).HasDatabaseName("ix_payment_attempts_payment_id");
    }
}
