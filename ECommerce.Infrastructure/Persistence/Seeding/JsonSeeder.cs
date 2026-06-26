using System.Reflection;
using System.Text.Json;
using ECommerce.Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Seeding;

public static class JsonSeeder
{
    public static async Task SeedFromJsonAsync<TEntity>(StoreDbContext context, string jsonFileName) where TEntity : class
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"ECommerce.Infrastructure.Persistence.Seeding.Data.{jsonFileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
            return;

        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var entities = JsonSerializer.Deserialize<List<TEntity>>(json);

        if (entities is null || entities.Count == 0)
            return;

        var dbSet = context.Set<TEntity>();

        if (await dbSet.AnyAsync())
            return;

        await dbSet.AddRangeAsync(entities);
        await context.SaveChangesAsync();
    }
}