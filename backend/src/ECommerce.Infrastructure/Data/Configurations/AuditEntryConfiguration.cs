using ECommerce.Domain.Audit;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");

        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).UseIdentityColumn();

        builder.Property(entry => entry.ActorId).HasColumnName("actor_id");
        builder.Property(entry => entry.ActorType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasColumnName("actor_type")
            .IsRequired();

        builder.Property(entry => entry.Action)
            .HasMaxLength(100)
            .HasColumnName("action")
            .IsRequired();

        builder.Property(entry => entry.EntityType)
            .HasMaxLength(100)
            .HasColumnName("entity_type")
            .IsRequired();

        builder.Property(entry => entry.EntityId)
            .HasMaxLength(64)
            .HasColumnName("entity_id");

        builder.Property(entry => entry.Before)
            .HasColumnName("before");

        builder.Property(entry => entry.After)
            .HasColumnName("after");

        builder.Property(entry => entry.Ip)
            .HasMaxLength(64)
            .HasColumnName("ip");

        builder.Property(entry => entry.UserAgent)
            .HasMaxLength(512)
            .HasColumnName("user_agent");

        builder.Property(entry => entry.TraceId)
            .HasMaxLength(64)
            .HasColumnName("trace_id");

        builder.Property(entry => entry.Hash)
            .HasMaxLength(64)
            .HasColumnName("hash")
            .IsRequired();

        builder.Property(entry => entry.PreviousHash)
            .HasMaxLength(64)
            .HasColumnName("previous_hash");

        builder.Property(entry => entry.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.HasIndex(entry => entry.ActorId);
        builder.HasIndex(entry => entry.Action);
        builder.HasIndex(entry => entry.OccurredAt);
    }
}
