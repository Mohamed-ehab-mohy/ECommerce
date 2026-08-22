using ECommerce.Domain.Catalog;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");

        builder.HasKey(variant => variant.Id);
        builder.Property(variant => variant.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(variant => variant.ProductId).IsRequired().HasColumnName("product_id");
        builder.Property(variant => variant.Sku)
            .HasMaxLength(50)
            .IsRequired()
            .HasColumnName("sku");
        builder.HasIndex(variant => variant.Sku)
            .IsUnique()
            .HasDatabaseName("ux_product_variants_sku");

        builder.Property(variant => variant.Name)
            .HasMaxLength(255)
            .IsRequired()
            .HasColumnName("name");

        builder.Property(variant => variant.Attributes)
            .HasColumnType("jsonb")
            .HasColumnName("attributes");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(variant => variant.ProductId);

        builder.HasIndex(variant => variant.ProductId).HasDatabaseName("ix_product_variants_product_id");

        builder.Property(variant => variant.CreatedAt).HasColumnName("created_at");
        builder.Property(variant => variant.UpdatedAt).HasColumnName("updated_at");
        builder.Property(variant => variant.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(variant => variant.DomainEvents);
    }
}
