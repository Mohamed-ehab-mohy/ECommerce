# SQL Server Provider

## Overview

Multi-database support: PostgreSQL (primary) and SQL Server, selectable via `DataProvider:Provider` configuration. Both EF Core and Dapper read models support provider switching.

## Configuration

```json
{
  "DataProvider": {
    "Provider": "Postgres"
  },
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5433;Database=ecommerce;Username=ecommerce;Password=...",
    "SqlServer": "Server=localhost,1433;Database=ecommerce;User Id=sa;Password=...;TrustServerCertificate=true"
  },
  "QueryProvider": {
    "Provider": "Ef"
  }
}
```

## Provider Switch (EF Core)

`Infrastructure.DependencyInjection` selects the provider at startup:

```csharp
if (string.Equals(dataProvider, "SqlServer", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(sqlServerCs))
{
    options.UseSqlServer(sqlServerCs);
}
else
{
    options.UseNpgsql(postgresCs);
}
```

## Provider Switch (Dapper)

`DapperReadModelStore` selects the connection type:

```csharp
return _provider switch
{
    "SqlServer" => new SqlConnection(_connectionString),
    _ => new NpgsqlConnection(_connectionString)
};
```

## Schema Differences

| Feature | PostgreSQL | SQL Server |
|---------|-----------|------------|
| Serial/Identity | `GENERATED ALWAYS AS IDENTITY` | `IDENTITY(1,1)` |
| JSON column type | `jsonb` | `nvarchar(max)` |
| Array support | Native `text[]` | No equivalent (JSON workaround) |
| Full-text search | `tsvector` / ` plainto_tsquery` | `FULLTEXT` index |
| Partitioning | Declarative (`PARTITION BY`) | Filegroups |
| Row-level security | `ENABLE ROW LEVEL SECURITY` | Row-Level Security Filter Predicates |

## Integration Tests

Tests run against both providers in CI:

```bash
# PostgreSQL (default)
dotnet test --filter "Category=Integration"

# SQL Server (override)
DataProvider__Provider=SqlServer dotnet test --filter "Category=Integration"
```

## Migration Strategy

- Keep provider-specific migration folders: `Migrations/Postgres/`, `Migrations/SqlServer/`
- Shared model defined in `ECommerceDbContext`; provider-specific SQL handled by EF Core conventions
- Seeds and stored procedures may require provider-specific implementations
