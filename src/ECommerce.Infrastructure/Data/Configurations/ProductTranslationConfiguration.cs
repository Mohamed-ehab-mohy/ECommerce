using ECommerce.Domain.Catalog;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class ProductTranslationConfiguration : IEntityTypeConfiguration<ProductTranslation>
{
    public void Configure(EntityTypeBuilder<ProductTranslation> builder)
    {
        builder.ToTable("product_translations");

        builder.HasKey(translation => new { translation.ProductId, translation.Locale });

        builder.Property(translation => translation.ProductId).IsRequired().HasColumnName("product_id");

        builder.Property(translation => translation.Locale)
            .HasMaxLength(5)
            .IsRequired()
            .HasColumnName("locale");

        builder.Property(translation => translation.Name)
            .HasMaxLength(255)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(translation => translation.Description)
            .HasColumnName("description");

        builder.Property(translation => translation.MetaTitle)
            .HasMaxLength(255)
            .HasColumnName("meta_title");

        builder.Property(translation => translation.MetaDescription)
            .HasMaxLength(512)
            .HasColumnName("meta_description");

        builder.HasOne<Product>()
            .WithMany(product => product.Translations)
            .HasForeignKey(translation => translation.ProductId);

        builder.HasIndex(translation => translation.Locale).HasDatabaseName("ix_product_translations_locale");
    }
}
