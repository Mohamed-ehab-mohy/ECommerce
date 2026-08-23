using ECommerce.Domain.Catalog;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands");

        builder.HasKey(brand => brand.Id);
        builder.Property(brand => brand.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(brand => brand.Name)
            .HasMaxLength(160)
            .IsRequired()
            .HasColumnName("name");
        builder.HasIndex(brand => brand.Name)
            .IsUnique()
            .HasDatabaseName("ux_brands_name");

        builder.Property(brand => brand.Description).HasColumnName("description");
        builder.Property(brand => brand.Website)
            .HasMaxLength(255)
            .HasColumnName("website");

        builder.Property(brand => brand.CreatedAt).HasColumnName("created_at");
        builder.Property(brand => brand.UpdatedAt).HasColumnName("updated_at");
        builder.Property(brand => brand.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(brand => brand.DomainEvents);
    }
}
