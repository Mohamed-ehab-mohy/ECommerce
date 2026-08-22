using ECommerce.Domain.Flags;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("feature_flags");

        builder.HasKey(flag => flag.Id);
        builder.Property(flag => flag.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(flag => flag.Key)
            .HasMaxLength(100)
            .IsRequired()
            .HasColumnName("key");
        builder.HasIndex(flag => flag.Key)
            .IsUnique()
            .HasDatabaseName("ux_feature_flags_key");

        builder.Property(flag => flag.Description)
            .HasMaxLength(500)
            .HasColumnName("description");

        builder.Property(flag => flag.Enabled)
            .IsRequired()
            .HasColumnName("enabled");

        builder.Property(flag => flag.CreatedAt).HasColumnName("created_at");
        builder.Property(flag => flag.UpdatedAt).HasColumnName("updated_at");
        builder.Property(flag => flag.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(flag => flag.DomainEvents);
    }
}
