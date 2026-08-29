using ECommerce.Domain.Content;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("pages");

        builder.HasKey(page => page.Id);
        builder.Property(page => page.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(page => page.Title)
            .HasMaxLength(255)
            .IsRequired()
            .HasColumnName("title");

        builder.Property(page => page.Slug)
            .HasMaxLength(160)
            .IsRequired()
            .HasColumnName("slug");
        builder.HasIndex(page => page.Slug)
            .HasDatabaseName("ix_pages_slug");

        builder.Property(page => page.HtmlContent)
            .HasColumnName("html_content");

        builder.Property(page => page.MetaTitle)
            .HasMaxLength(255)
            .HasColumnName("meta_title");

        builder.Property(page => page.MetaDescription)
            .HasMaxLength(512)
            .HasColumnName("meta_description");

        builder.Property(page => page.IsPublished)
            .HasColumnName("is_published");

        builder.Property(page => page.CreatedAt).HasColumnName("created_at");
        builder.Property(page => page.UpdatedAt).HasColumnName("updated_at");
        builder.Property(page => page.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(page => page.DomainEvents);
    }
}
