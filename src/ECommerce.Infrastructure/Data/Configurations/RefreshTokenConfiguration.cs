using ECommerce.Domain.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever().HasColumnName("id");

        builder.Property(token => token.UserId).IsRequired().HasColumnName("user_id");
        builder.Property(token => token.FamilyId).IsRequired().HasColumnName("family_id");
        builder.Property(token => token.DeviceId).HasMaxLength(128).IsRequired().HasColumnName("device_id");
        builder.Property(token => token.TokenHash).HasMaxLength(128).IsRequired().HasColumnName("token_hash");
        builder.Property(token => token.ExpiresAtUtc).IsRequired().HasColumnName("expires_at");
        builder.Property(token => token.RevokedAtUtc).HasColumnName("revoked_at");
        builder.Property(token => token.ReplacedById).HasColumnName("replaced_by_id");

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.UserId);
        builder.HasIndex(token => token.FamilyId);

        builder.Property(token => token.CreatedAt).HasColumnName("created_at");
        builder.Property(token => token.UpdatedAt).HasColumnName("updated_at");
        builder.Property(token => token.IsDeleted).HasColumnName("is_deleted");

        builder.Ignore(token => token.DomainEvents);
    }
}
