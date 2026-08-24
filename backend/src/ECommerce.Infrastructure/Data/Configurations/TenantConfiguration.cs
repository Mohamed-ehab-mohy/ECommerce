using ECommerce.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Data.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Subdomain).IsRequired().HasMaxLength(63);
        builder.Property(x => x.CustomDomain).HasMaxLength(255);

        builder.HasIndex(x => x.Subdomain).IsUnique();
        builder.HasIndex(x => x.CustomDomain).IsUnique();

        builder.HasOne(x => x.Settings)
            .WithOne()
            .HasForeignKey<TenantSettings>(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
