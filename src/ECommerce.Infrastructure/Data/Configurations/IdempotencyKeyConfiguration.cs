using ECommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("idempotency_keys");

        builder.HasKey(idempotencyKey => idempotencyKey.Id);
        builder.Property(idempotencyKey => idempotencyKey.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(idempotencyKey => idempotencyKey.Key)
            .HasMaxLength(128)
            .IsRequired()
            .HasColumnName("key");

        builder.Property(idempotencyKey => idempotencyKey.CheckoutId)
            .IsRequired()
            .HasColumnName("checkout_id");

        builder.Property(idempotencyKey => idempotencyKey.OrderId)
            .IsRequired()
            .HasColumnName("order_id");

        builder.Property(idempotencyKey => idempotencyKey.CreatedAt).HasColumnName("created_at");
        builder.Property(idempotencyKey => idempotencyKey.UpdatedAt).HasColumnName("updated_at");
        builder.Property(idempotencyKey => idempotencyKey.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(idempotencyKey => idempotencyKey.DomainEvents);

        builder.HasIndex(idempotencyKey => idempotencyKey.Key)
            .IsUnique()
            .HasDatabaseName("ux_idempotency_keys_key");
    }
}
