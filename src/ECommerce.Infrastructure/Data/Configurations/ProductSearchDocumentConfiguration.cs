using ECommerce.Domain.Catalog;
using ECommerce.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class ProductSearchDocumentConfiguration : IEntityTypeConfiguration<ProductSearchDocument>
{
    public void Configure(EntityTypeBuilder<ProductSearchDocument> builder)
    {
        builder.ToTable("product_search_documents");

        builder.HasKey(document => new { document.ProductId, document.Locale });

        builder.Property(document => document.ProductId).HasColumnName("product_id");
        builder.Property(document => document.Locale).HasColumnName("locale").HasMaxLength(10).IsRequired();
        builder.Property(document => document.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(document => document.Description).HasColumnName("description");
        builder.Property(document => document.Sku).HasColumnName("sku").HasMaxLength(50).IsRequired();
        builder.Property(document => document.Brand).HasColumnName("brand").HasMaxLength(255);
        builder.Property(document => document.BrandId).HasColumnName("brand_id");
        builder.Property(document => document.Category).HasColumnName("category").HasMaxLength(255);
        builder.Property(document => document.CategoryId).HasColumnName("category_id");
        builder.Property(document => document.ListAmount).HasColumnName("list_amount").HasColumnType("numeric(18,4)");
        builder.Property(document => document.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(document => document.RatingAverage).HasColumnName("rating_average").HasColumnType("numeric(3,2)");
        builder.Property(document => document.RatingCount).HasColumnName("rating_count");

        builder.Property(document => document.SearchVector)
            .HasColumnName("search_vector")
            .HasComputedColumnSql(
                """
                setweight(to_tsvector('simple', coalesce(name, '')), 'A') ||
                setweight(to_tsvector('simple', coalesce(description, '')), 'B') ||
                setweight(to_tsvector('simple', coalesce(brand, '')), 'C') ||
                setweight(to_tsvector('simple', coalesce(sku, '')), 'D')
                """,
                stored: true);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(document => document.ProductId)
            .HasConstraintName("fk_product_search_documents_product")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(document => document.SearchVector)
            .HasDatabaseName("ix_product_search_documents_search_vector")
            .HasMethod("GIN");

        builder.HasIndex(document => document.Name)
            .HasDatabaseName("ix_product_search_documents_name_trgm")
            .HasMethod("GIN")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(document => new { document.Locale, document.CategoryId })
            .HasDatabaseName("ix_product_search_documents_locale_category");

        builder.HasIndex(document => new { document.Locale, document.BrandId })
            .HasDatabaseName("ix_product_search_documents_locale_brand");

        builder.HasIndex(document => new { document.Locale, document.ListAmount })
            .HasDatabaseName("ix_product_search_documents_locale_price");
    }
}
