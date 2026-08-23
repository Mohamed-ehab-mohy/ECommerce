using ECommerce.Domain.Notifications;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("notification_preferences");

        builder.HasKey(preference => preference.Id);
        builder.Property(preference => preference.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(preference => preference.CustomerId)
            .IsRequired()
            .HasColumnName("customer_id");

        builder.Property(preference => preference.Channel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("channel");

        builder.Property(preference => preference.Kind)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnName("kind");

        builder.Property(preference => preference.Enabled)
            .IsRequired()
            .HasColumnName("enabled");

        builder.Property(preference => preference.CreatedAt).HasColumnName("created_at");
        builder.Property(preference => preference.UpdatedAt).HasColumnName("updated_at");
        builder.Property(preference => preference.IsDeleted).HasColumnName("is_deleted");

        builder.HasIndex(preference => new { preference.CustomerId, preference.Channel, preference.Kind })
            .IsUnique()
            .HasDatabaseName("ux_notification_preferences_customer_channel_kind");

        builder.Ignore(preference => preference.DomainEvents);
    }
}
