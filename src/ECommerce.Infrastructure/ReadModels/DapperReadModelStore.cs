using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ECommerce.Infrastructure.ReadModels;

public sealed class DapperReadModelStore : IReadModelStore, IDbConnectionFactory
{
    private readonly string _connectionString;

    public DapperReadModelStore(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Postgres")!;
    }

    public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var results = await connection.QueryAsync<T>(sql, parameters);
        return results.ToList();
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        return await connection.ExecuteAsync(sql, parameters);
    }
}
