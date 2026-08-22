using ECommerce.Domain.Checkout;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class CheckoutSagaStateConfiguration : IEntityTypeConfiguration<CheckoutSagaState>
{
    public void Configure(EntityTypeBuilder<CheckoutSagaState> builder)
    {
        builder.ToTable("checkout_saga_states");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.CorrelationId).HasColumnName("correlation_id");
        builder.Property(s => s.CurrentState).HasColumnName("current_state");
        builder.Property(s => s.CheckoutId).HasColumnName("checkout_id");
        builder.Property(s => s.OrderId).HasColumnName("order_id");
        builder.Property(s => s.PaymentId).HasColumnName("payment_id");
        builder.Property(s => s.CustomerId).HasColumnName("customer_id");
        builder.Property(s => s.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        builder.Property(s => s.RetryCount).HasColumnName("retry_count");
        builder.Property(s => s.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200);

        builder.Ignore(s => s.DomainEvents);
    }
}
