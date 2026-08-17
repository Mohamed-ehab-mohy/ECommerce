namespace ECommerce.Infrastructure.ReadModels;

public interface IReadModelStore
{
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default);
    Task<int> ExecuteAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);
}

public interface IDbConnectionFactory
{
    System.Data.IDbConnection CreateConnection();
}
