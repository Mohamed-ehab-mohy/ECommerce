using ECommerce.Domain.Content;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.ToTable("banners");

        builder.HasKey(banner => banner.Id);
        builder.Property(banner => banner.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(banner => banner.Title)
            .HasMaxLength(255)
            .IsRequired()
            .HasColumnName("title");

        builder.Property(banner => banner.ImageUrl)
            .HasMaxLength(2048)
            .IsRequired()
            .HasColumnName("image_url");

        builder.Property(banner => banner.TargetUrl)
            .HasMaxLength(2048)
            .HasColumnName("target_url");

        builder.Property(banner => banner.DisplayOrder)
            .HasColumnName("display_order");

        builder.Property(banner => banner.IsActive)
            .HasColumnName("is_active");

        builder.Property(banner => banner.CreatedAt).HasColumnName("created_at");
        builder.Property(banner => banner.UpdatedAt).HasColumnName("updated_at");
        builder.Property(banner => banner.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(banner => banner.DomainEvents);
    }
}
