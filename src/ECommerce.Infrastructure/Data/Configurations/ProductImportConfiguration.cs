using ECommerce.Domain.Catalog;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class ProductImportConfiguration : IEntityTypeConfiguration<ProductImport>
{
    public void Configure(EntityTypeBuilder<ProductImport> builder)
    {
        builder.ToTable("product_imports");

        builder.HasKey(import => import.Id);
        builder.Property(import => import.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(import => import.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasColumnName("status");

        builder.Property(import => import.RowsJson)
            .HasColumnType("text")
            .IsRequired()
            .HasColumnName("rows_json");

        builder.Property(import => import.TotalRows).HasColumnName("total_rows");
        builder.Property(import => import.SucceededRows).HasColumnName("succeeded_rows");
        builder.Property(import => import.FailedRows).HasColumnName("failed_rows");
        builder.Property(import => import.StartedAtUtc).HasColumnName("started_at_utc");
        builder.Property(import => import.FinishedAtUtc).HasColumnName("finished_at_utc");

        builder.HasMany(import => import.Errors)
            .WithOne()
            .HasForeignKey(error => error.ProductImportId)
            .HasConstraintName("fk_product_import_errors_product_imports");

        builder.Property(import => import.CreatedAt).HasColumnName("created_at");
        builder.Property(import => import.UpdatedAt).HasColumnName("updated_at");
        builder.Property(import => import.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(import => import.DomainEvents);
    }
}
