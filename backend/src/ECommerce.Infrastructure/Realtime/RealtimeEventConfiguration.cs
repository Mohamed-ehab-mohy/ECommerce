using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Realtime;

public sealed class RealtimeEventConfiguration : IEntityTypeConfiguration<RealtimeEvent>
{
    public void Configure(EntityTypeBuilder<RealtimeEvent> builder)
    {
        builder.ToTable("realtime_events");

        builder.HasKey(realtimeEvent => realtimeEvent.Id);
        builder.Property(realtimeEvent => realtimeEvent.Id)
            .ValueGeneratedOnAdd()
            .HasColumnName("id");

        builder.Property(realtimeEvent => realtimeEvent.GroupKey)
            .HasMaxLength(64)
            .HasColumnName("group_key")
            .IsRequired();

        builder.Property(realtimeEvent => realtimeEvent.Type)
            .HasMaxLength(64)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(realtimeEvent => realtimeEvent.DataJson)
            .HasColumnType("jsonb")
            .HasColumnName("data_json")
            .IsRequired();

        builder.Property(realtimeEvent => realtimeEvent.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.HasIndex(realtimeEvent => new { realtimeEvent.GroupKey, realtimeEvent.Id });
    }
}
