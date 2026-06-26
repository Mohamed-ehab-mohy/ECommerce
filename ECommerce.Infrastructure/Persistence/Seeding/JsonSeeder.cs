using System.Text.Json;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Seeding;

public static class JsonSeeder
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static async Task SeedIfEmptyAsync<TEntity, TModel>(
        DbSet<TEntity> dbSet,
        string fileName,
        Func<TModel, TEntity> map,
        CancellationToken ct = default) where TEntity : BaseEntity
    {
        if (await dbSet.AnyAsync(ct))
            return;

        var filePath = Path.Combine(AppContext.BaseDirectory, "Persistence", "Seeding", "Data", fileName);

        if (!File.Exists(filePath))
            return;

        using var stream = File.OpenRead(filePath);
        var models = await JsonSerializer.DeserializeAsync<List<TModel>>(stream, Options, ct);

        if (models is null || models.Count == 0)
            return;

        var entities = models.Select(map);

        await dbSet.AddRangeAsync(entities, ct);
    }
}