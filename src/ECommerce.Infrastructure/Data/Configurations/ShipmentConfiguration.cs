using ECommerce.Domain.Fulfillment;
using ECommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");

        builder.HasKey(shipment => shipment.Id);
        builder.Property(shipment => shipment.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(shipment => shipment.OrderId)
            .IsRequired()
            .HasColumnName("order_id");
        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(shipment => shipment.OrderId)
            .HasConstraintName("fk_shipments_orders");

        builder.Property(shipment => shipment.FulfillmentTaskId)
            .IsRequired()
            .HasColumnName("fulfillment_task_id");
        builder.HasOne<FulfillmentTask>()
            .WithMany()
            .HasForeignKey(shipment => shipment.FulfillmentTaskId)
            .HasConstraintName("fk_shipments_fulfillment_tasks");
        builder.HasIndex(shipment => shipment.FulfillmentTaskId)
            .HasDatabaseName("ix_shipments_fulfillment_task_id");

        builder.Property(shipment => shipment.CarrierKey)
            .HasMaxLength(32)
            .IsRequired()
            .HasColumnName("carrier_key");

        builder.Property(shipment => shipment.TrackingNumber)
            .HasMaxLength(64)
            .IsRequired()
            .HasColumnName("tracking_number");
        builder.HasIndex(shipment => shipment.TrackingNumber)
            .IsUnique()
            .HasDatabaseName("ux_shipments_tracking_number");

        builder.Property(shipment => shipment.LabelUrl)
            .HasMaxLength(512)
            .HasColumnName("label_url");

        builder.Property(shipment => shipment.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(shipment => shipment.ShippedAt)
            .IsRequired()
            .HasColumnName("shipped_at");

        builder.Property(shipment => shipment.DeliveredAt)
            .HasColumnName("delivered_at");

        builder.Property(shipment => shipment.CreatedAt).HasColumnName("created_at");
        builder.Property(shipment => shipment.UpdatedAt).HasColumnName("updated_at");
        builder.Property(shipment => shipment.IsDeleted).HasColumnName("is_deleted");

        builder.HasMany(shipment => shipment.Updates)
            .WithOne()
            .HasForeignKey(update => update.ShipmentId)
            .HasConstraintName("fk_tracking_updates_shipments");

        builder.HasIndex(shipment => shipment.OrderId).HasDatabaseName("ix_shipments_order_id");

        builder.Ignore(shipment => shipment.DomainEvents);
    }
}
