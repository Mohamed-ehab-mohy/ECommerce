using ECommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class OrderStatusLogConfiguration : IEntityTypeConfiguration<OrderStatusLog>
{
    public void Configure(EntityTypeBuilder<OrderStatusLog> builder)
    {
        builder.ToTable("order_status_log");

        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(entry => entry.OrderId).HasColumnName("order_id");

        builder.Property(entry => entry.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(24)
            .HasColumnName("from_status");

        builder.Property(entry => entry.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired()
            .HasColumnName("to_status");

        builder.Property(entry => entry.ActorType)
            .HasMaxLength(24)
            .IsRequired()
            .HasColumnName("actor_type");

        builder.Property(entry => entry.ActorId).HasColumnName("actor_id");

        builder.Property(entry => entry.TraceId)
            .HasMaxLength(64)
            .HasColumnName("trace_id");

        builder.Property(entry => entry.OccurredAt).HasColumnName("occurred_at");

        builder.HasIndex(entry => entry.OrderId).HasDatabaseName("ix_order_status_log_order_id");
    }
}
