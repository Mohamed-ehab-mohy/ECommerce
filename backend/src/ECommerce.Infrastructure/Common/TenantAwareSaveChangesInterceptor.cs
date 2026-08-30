using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ECommerce.Infrastructure.Common;

public sealed class TenantAwareSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        SetTenantIds(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        SetTenantIds(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SetTenantIds(DbContext? context)
    {
        if (context is not ECommerceDbContext ecommerceContext || ecommerceContext.CurrentTenant is not { } tenantId)
            return;

        foreach (var entry in context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added && e.Properties.Any(p => p.Metadata.Name == "TenantId")))
        {
            var tenantProp = entry.Properties.First(p => p.Metadata.Name == "TenantId");
            if (tenantProp.CurrentValue is null)
            {
                tenantProp.CurrentValue = tenantId;
            }
        }
    }
}
