using ECommerce.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

internal sealed class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.ToTable("TenantSettings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DefaultCurrency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.DefaultLocale).IsRequired().HasMaxLength(10);
        builder.Property(x => x.ThemeName).HasMaxLength(50);
        builder.Property(x => x.LogoUrl).HasMaxLength(2000);
    }
}
