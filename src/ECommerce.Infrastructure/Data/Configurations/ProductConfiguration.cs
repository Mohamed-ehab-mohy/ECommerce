using ECommerce.Domain.Catalog;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(product => product.Sku)
            .HasMaxLength(50)
            .IsRequired()
            .HasColumnName("sku");
        builder.HasIndex(product => product.Sku)
            .IsUnique()
            .HasDatabaseName("ux_products_sku");

        builder.Property(product => product.Slug)
            .HasMaxLength(160)
            .IsRequired()
            .HasColumnName("slug");
        builder.HasIndex(product => product.Slug)
            .IsUnique()
            .HasDatabaseName("ux_products_slug");

        builder.Property(product => product.CategoryId).HasColumnName("category_id");
        builder.Property(product => product.BrandId).HasColumnName("brand_id");

        builder.Property(product => product.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(product => product.IsFeatured).HasColumnName("is_featured");

        builder.Property(product => product.Backorderable).HasColumnName("backorderable");

        builder.Property(product => product.ImageUrls)
            .HasColumnType("jsonb")
            .HasColumnName("image_urls")
            .IsRequired();

        builder.Property(product => product.Attributes)
            .HasColumnType("jsonb")
            .HasColumnName("attributes");

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(product => product.CategoryId);

        builder.HasOne<Brand>()
            .WithMany()
            .HasForeignKey(product => product.BrandId);

        builder.HasIndex(product => product.CategoryId).HasDatabaseName("ix_products_category_id");
        builder.HasIndex(product => product.BrandId).HasDatabaseName("ix_products_brand_id");
        builder.HasIndex(product => product.Status).HasDatabaseName("ix_products_status");

        builder.Property(product => product.CreatedAt).HasColumnName("created_at");
        builder.Property(product => product.UpdatedAt).HasColumnName("updated_at");
        builder.Property(product => product.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(product => product.DomainEvents);
    }
}
