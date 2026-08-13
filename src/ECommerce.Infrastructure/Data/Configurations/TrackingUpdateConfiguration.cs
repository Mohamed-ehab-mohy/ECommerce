using ECommerce.Domain.Fulfillment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class TrackingUpdateConfiguration : IEntityTypeConfiguration<TrackingUpdate>
{
    public void Configure(EntityTypeBuilder<TrackingUpdate> builder)
    {
        builder.ToTable("tracking_updates");

        builder.HasKey(update => update.Id);
        builder.Property(update => update.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(update => update.ShipmentId)
            .IsRequired()
            .HasColumnName("shipment_id");
        builder.HasOne<Shipment>()
            .WithMany(shipment => shipment.Updates)
            .HasForeignKey(update => update.ShipmentId)
            .HasConstraintName("fk_tracking_updates_shipments");

        builder.Property(update => update.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(update => update.OccurredAt)
            .IsRequired()
            .HasColumnName("occurred_at");

        builder.Property(update => update.Note)
            .HasMaxLength(512)
            .HasColumnName("note");

        builder.HasIndex(update => update.ShipmentId).HasDatabaseName("ix_tracking_updates_shipment_id");
    }
}
