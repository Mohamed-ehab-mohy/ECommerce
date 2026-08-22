using ECommerce.Domain.Catalog;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class ProductPriceConfiguration : IEntityTypeConfiguration<ProductPrice>
{
    public void Configure(EntityTypeBuilder<ProductPrice> builder)
    {
        builder.ToTable("product_prices");

        builder.HasKey(price => new { price.ProductId, price.Currency });

        builder.Property(price => price.ProductId).IsRequired().HasColumnName("product_id");

        builder.Property(price => price.Currency)
            .HasMaxLength(3)
            .IsRequired()
            .HasColumnName("currency");

        builder.Property(price => price.ListAmount)
            .HasPrecision(18, 4)
            .IsRequired()
            .HasColumnName("list_amount");

        builder.Property(price => price.OfferAmount)
            .HasPrecision(18, 4)
            .HasColumnName("offer_amount");

        builder.Property(price => price.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<Product>()
            .WithMany(product => product.Prices)
            .HasForeignKey(price => price.ProductId);

        builder.HasIndex(price => new { price.Currency, price.ListAmount })
            .HasDatabaseName("ix_product_prices_currency_list_amount");

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_product_prices_list_positive",
            "\"list_amount\" > 0"));

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_product_prices_offer_not_above_list",
            "\"offer_amount\" IS NULL OR \"offer_amount\" <= \"list_amount\""));
    }
}
