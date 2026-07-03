using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ECommerce.Infrastructure.Persistence.Interceptors;

public sealed class SoftDeleteInterceptor
{
    public void ApplySoftDelete(DbContext context)
    {
        foreach (EntityEntry<BaseEntity> entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State is EntityState.Deleted)
            {
                entry.Entity.MarkAsDeleted();
                entry.State = EntityState.Modified;
            }
        }
    }
}
