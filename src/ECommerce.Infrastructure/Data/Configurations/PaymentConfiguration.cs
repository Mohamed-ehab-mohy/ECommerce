using ECommerce.Domain.Payments;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(payment => payment.OrderId).HasColumnName("order_id");
        builder.Property(payment => payment.CustomerId).HasColumnName("customer_id");

        builder.Property(payment => payment.ProviderKey)
            .HasMaxLength(30)
            .IsRequired()
            .HasColumnName("provider_key");

        builder.Property(payment => payment.ProviderToken)
            .HasColumnType("text")
            .IsRequired()
            .HasColumnName("provider_token");

        builder.Property(payment => payment.ClientToken)
            .HasColumnType("text")
            .IsRequired()
            .HasColumnName("client_token");

        builder.Property(payment => payment.ProviderReference)
            .HasMaxLength(120)
            .HasColumnName("provider_reference");

        builder.Property(payment => payment.Currency)
            .HasMaxLength(3)
            .IsRequired()
            .HasColumnName("currency");

        builder.Property(payment => payment.Amount)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("amount");

        builder.Property(payment => payment.FxRate)
            .HasColumnType("decimal(18,8)")
            .HasColumnName("fx_rate");

        builder.Property(payment => payment.AuthorizedAmount)
            .HasColumnType("decimal(18,4)")
            .IsRequired()
            .HasColumnName("authorized_amount");

        builder.Property(payment => payment.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(payment => payment.AuthorizedAt).HasColumnName("authorized_at");
        builder.Property(payment => payment.CapturedAt).HasColumnName("captured_at");
        builder.Property(payment => payment.VoidedAt).HasColumnName("voided_at");
        builder.Property(payment => payment.Attempt).HasColumnName("attempt");
        builder.Property(payment => payment.RetryAfterUtc).HasColumnName("retry_after_utc");

        builder.Property(payment => payment.CreatedAt).HasColumnName("created_at");
        builder.Property(payment => payment.UpdatedAt).HasColumnName("updated_at");
        builder.Property(payment => payment.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(payment => payment.DomainEvents);

        builder.HasMany(payment => payment.Attempts)
            .WithOne()
            .HasForeignKey(attempt => attempt.PaymentId)
            .HasConstraintName("fk_payment_attempts_payments");

        builder.HasMany(payment => payment.Ledger)
            .WithOne()
            .HasForeignKey(entry => entry.PaymentId)
            .HasConstraintName("fk_payment_ledger_payments");

        builder.HasIndex(payment => payment.OrderId).HasDatabaseName("ux_payments_order_id");
        builder.HasIndex(payment => payment.ProviderReference).HasDatabaseName("ix_payments_provider_reference");
    }
}
