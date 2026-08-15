using ECommerce.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class ProductImportErrorConfiguration : IEntityTypeConfiguration<ProductImportError>
{
    public void Configure(EntityTypeBuilder<ProductImportError> builder)
    {
        builder.ToTable("product_import_errors");

        builder.HasKey(error => error.Id);
        builder.Property(error => error.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(error => error.ProductImportId).HasColumnName("product_import_id");
        builder.Property(error => error.RowNumber).HasColumnName("row_number");
        builder.Property(error => error.Sku).HasMaxLength(50).HasColumnName("sku");
        builder.Property(error => error.Message).HasMaxLength(1000).HasColumnName("message");
        builder.Property(error => error.CreatedAt).HasColumnName("created_at");
        builder.Property(error => error.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(error => error.ProductImportId).HasDatabaseName("ix_product_import_errors_product_import_id");
    }
}
