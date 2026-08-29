using ECommerce.Domain.Content;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class CmsLayoutConfiguration : IEntityTypeConfiguration<CmsLayout>
{
    public void Configure(EntityTypeBuilder<CmsLayout> builder)
    {
        builder.ToTable("cms_layouts");

        builder.HasKey(layout => layout.Id);
        builder.Property(layout => layout.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(layout => layout.Name)
            .HasMaxLength(255)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(layout => layout.Slug)
            .HasMaxLength(160)
            .IsRequired()
            .HasColumnName("slug");
        builder.HasIndex(layout => layout.Slug)
            .HasDatabaseName("ix_cms_layouts_slug");

        builder.Property(layout => layout.IsActive)
            .HasColumnName("is_active");

        builder.Property(layout => layout.CreatedAt).HasColumnName("created_at");
        builder.Property(layout => layout.UpdatedAt).HasColumnName("updated_at");
        builder.Property(layout => layout.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(layout => layout.DomainEvents);
    }
}
