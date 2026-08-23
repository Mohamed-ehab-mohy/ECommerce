using ECommerce.Domain.Wishlist;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("wishlists");

        builder.HasKey(wishlist => wishlist.Id);
        builder.Property(wishlist => wishlist.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(wishlist => wishlist.OwnerKey)
            .HasMaxLength(64)
            .IsRequired()
            .HasColumnName("owner_key");
        builder.HasIndex(wishlist => wishlist.OwnerKey)
            .IsUnique()
            .HasDatabaseName("ux_wishlists_owner_key");

        builder.Property(wishlist => wishlist.CreatedAt).HasColumnName("created_at");
        builder.Property(wishlist => wishlist.UpdatedAt).HasColumnName("updated_at");

        builder.Ignore(wishlist => wishlist.DomainEvents);

        builder.HasMany(wishlist => wishlist.Items)
            .WithOne()
            .HasForeignKey(item => item.WishlistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
