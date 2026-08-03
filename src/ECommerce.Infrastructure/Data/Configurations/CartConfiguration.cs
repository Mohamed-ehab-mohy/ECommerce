using ECommerce.Domain.Cart;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("carts");

        builder.HasKey(cart => cart.Id);
        builder.Property(cart => cart.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(cart => cart.OwnerKey)
            .HasMaxLength(64)
            .IsRequired()
            .HasColumnName("owner_key");
        builder.HasIndex(cart => cart.OwnerKey)
            .IsUnique()
            .HasDatabaseName("ux_carts_owner_key");

        builder.Property(cart => cart.Currency)
            .HasMaxLength(3)
            .IsRequired()
            .HasColumnName("currency");

        builder.Property(cart => cart.Version)
            .IsConcurrencyToken()
            .HasColumnName("version");

        builder.Property(cart => cart.ExpiresAt).HasColumnName("expires_at");
        builder.Property(cart => cart.CreatedAt).HasColumnName("created_at");
        builder.Property(cart => cart.UpdatedAt).HasColumnName("updated_at");

        builder.Ignore(cart => cart.DomainEvents);

        builder.HasMany(cart => cart.Items)
            .WithOne()
            .HasForeignKey(item => item.CartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
