using ECommerce.Domain.Wishlist;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("wishlist_items");

        builder.HasKey(item => new { item.WishlistId, item.ProductId });

        builder.Property(item => item.WishlistId)
            .IsRequired()
            .HasColumnName("wishlist_id");

        builder.Property(item => item.ProductId)
            .IsRequired()
            .HasColumnName("product_id");

        builder.Property(item => item.AddedAt).HasColumnName("added_at");
    }
}
