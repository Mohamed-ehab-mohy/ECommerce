using ECommerce.Domain.Content;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class CmsLayoutSectionConfiguration : IEntityTypeConfiguration<CmsLayoutSection>
{
    public void Configure(EntityTypeBuilder<CmsLayoutSection> builder)
    {
        builder.ToTable("cms_layout_sections");

        builder.HasKey(section => section.Id);
        builder.Property(section => section.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(section => section.LayoutId)
            .IsRequired()
            .HasColumnName("layout_id");

        builder.Property(section => section.Title)
            .HasMaxLength(255)
            .IsRequired()
            .HasColumnName("title");

        builder.Property(section => section.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(section => section.DisplayOrder)
            .HasColumnName("display_order");

        builder.Property(section => section.ConfigJson)
            .HasColumnType("jsonb")
            .HasColumnName("config_json");

        builder.Property(section => section.IsActive)
            .HasColumnName("is_active");

        builder.Property(section => section.CreatedAt).HasColumnName("created_at");
        builder.Property(section => section.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<CmsLayout>()
            .WithMany(layout => layout.Sections)
            .HasForeignKey(section => section.LayoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(section => section.LayoutId).HasDatabaseName("ix_cms_layout_sections_layout_id");
    }
}
