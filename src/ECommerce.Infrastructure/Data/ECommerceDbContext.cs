using ECommerce.Domain.Audit;
using ECommerce.Domain.Catalog;
using ECommerce.Domain.Identity;
using ECommerce.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data;

public sealed class ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ProductTranslation> ProductTranslations => Set<ProductTranslation>();

    public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<CategoryHierarchy> CategoryHierarchy => Set<CategoryHierarchy>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ECommerceDbContext).Assembly);
    }
}
