# Document 38 — Database Migration Strategy

> **Platform:** E-Commerce Platform (`ECommerce`)
> **Document Type:** Database Migration Strategy & Operations
> **Status:** Draft v1.0
> **Audience:** Engineering, DevOps, DBA

---

## 1. Overview

The platform uses **Entity Framework Core** as its ORM and migration tool. The primary database is **PostgreSQL** via the `Npgsql` provider, with optional **SQL Server** support for enterprise deployments. Migrations are generated from the `ECommerceDbContext` model, applied automatically on application startup, and versioned with EF Core's timestamp-based migration naming.

### Key Source Files

| File | Purpose |
|------|---------|
| `Infrastructure/Data/ECommerceDbContext.cs` | DbContext with 40+ DbSet registrations |
| `Infrastructure/Data/ECommerceDbContextFactory.cs` | Design-time factory for `dotnet ef` CLI |
| `Infrastructure/Data/MigrateOnStartupHostedService.cs` | Auto-migration `BackgroundService` |
| `Infrastructure/DependencyInjection.cs:68–90` | Provider switching (Postgres vs SQL Server) |

---

## 2. EF Core Migration Workflow

### 2.1 Design-Time Context Factory

`ECommerceDbContextFactory` implements `IDesignTimeDbContextFactory<ECommerceDbContext>` to enable `dotnet ef` commands outside the running application. It reads the connection string from the `ConnectionStrings__Postgres` environment variable with a localhost fallback, and configures `NpgsqlDataSourceBuilder` with dynamic JSON support.

```
dotnet ef migrations add <MigrationName> \
  --project src/ECommerce.Infrastructure \
  --startup-project src/ECommerce.API
```

### 2.2 Migration Naming Convention

All migrations use EF Core's timestamp prefix (`yyyyMMddHHmmss_`) followed by a descriptive PascalCase name. Examples from the codebase:

| Migration | Purpose |
|-----------|---------|
| `20260731193628_InitialMigration` | Baseline schema |
| `20260801132413_AddIdentityAndOutbox` | Identity + outbox tables |
| `20260802151432_CatalogMigration` | Product, category, brand tables |
| `20260815175329_AddAnalyticsAndWebhooks` | Analytics and webhook tables |

### 2.3 Applying Migrations

```bash
# Apply all pending migrations
dotnet ef database update \
  --project src/ECommerce.Infrastructure \
  --startup-project src/ECommerce.API

# List applied migrations
dotnet ef migrations list \
  --project src/ECommerce.Infrastructure
```

---

## 3. Multi-Provider Strategy

### 3.1 Provider Selection

The provider is selected at runtime via the `DataProvider` configuration key in `DependencyInjection.cs:68–90`:

- **Postgres** (default): Uses `NpgsqlDataSourceBuilder` with `EnableDynamicJson()` and the `pg_trgm` extension.
- **SqlServer**: Uses `UseSqlServer()` when `DataProvider` equals `"SqlServer"` and a SQL Server connection string is provided.

### 3.2 Migration Folders

EF Core generates migrations into the default `Migrations/` folder under `ECommerce.Infrastructure`. For multi-provider scenarios, migrations must be generated separately per provider:

```bash
# Postgres migrations (default)
dotnet ef migrations add <Name> \
  --project src/ECommerce.Infrastructure \
  --startup-project src/ECommerce.API

# SQL Server migrations (requires provider-specific startup config)
dotnet ef migrations add <Name> \
  --project src/ECommerce.Infrastructure \
  --startup-project src/ECommerce.API \
  -- --provider SqlServer
```

### 3.3 PostgreSQL-Specific Features

The `OnModelCreating` method in `ECommerceDbContext.cs:124–128` configures:

```csharp
modelBuilder.HasPostgresExtension("pg_trgm");
modelBuilder.ApplyConfigurationsFromAssembly(typeof(ECommerceDbContext).Assembly);
```

The `pg_trgm` extension powers trigram-based product search (`ProductSearchDocument`). This extension is not available on SQL Server; equivalent full-text search must be configured separately when using the SQL Server provider.

---

## 4. Migration Versioning and Rollback

### 4.1 Versioning

EF Core tracks applied migrations in the `__EFMigrationsHistory` table. Each migration is identified by its unique timestamp+name combination. The model snapshot (`ECommerceDbContextModelSnapshot.cs`) is maintained alongside migrations.

### 4.2 Rollback Strategy

EF Core does not provide a built-in "down" command in production. Rollback is performed by reverting to a previous migration snapshot:

```bash
# Rollback to a specific migration
dotnet ef database update <PreviousMigrationName> \
  --project src/ECommerce.Infrastructure \
  --startup-project src/ECommerce.API
```

For destructive rollbacks (dropping tables/columns), a manual SQL script should be generated:

```bash
dotnet ef migrations script \
  --from <TargetMigration> \
  --to <CurrentMigration> \
  --idempotent \
  --output rollback.sql
```

### 4.3 Breaking Changes Protocol

Schema changes that cannot be rolled back (column drops, type changes) require:

1. A forward-only migration that is backward-compatible
2. A deployment phase where both old and new code can read the schema
3. A follow-up migration to remove deprecated columns after full rollout

---

## 5. Seed Data via InsertData

### 5.1 Pattern

EF Core's `HasData` / `InsertData` is used within migrations to seed reference data. Seeded data is tracked by the migration and applied idempotently.

### 5.2 Existing Seed Examples

| Migration | Seeded Data |
|-----------|-------------|
| `20260814133354_SeedNotificationFlags` | Default feature flags for notification channels |
| `20260811102546_GrantPromotionsPermissions` | Role-permission mappings for promotions |
| `20260813161453_GrantFulfillmentPermissions` | Role-permission mappings for fulfillment |

### 5.3 Creating Seed Migrations

```csharp
migrationBuilder.InsertData(
    table: "FeatureFlags",
    columns: new[] { "Key", "Description", "Enabled" },
    values: new object[] { "notifications.email.enabled", "Enable email notifications", true });
```

Seed data should be committed alongside the migration file and reviewed as part of the PR process. Avoid seeding mutable data (e.g., products, prices) via migrations — use application-level initialization for those.

---

## 6. Auto-Migration on Startup

### 6.1 MigrateOnStartupHostedService

`MigrateOnStartupHostedService` (`Infrastructure/Data/MigrateOnStartupHostedService.cs`) is a `BackgroundService` that applies all pending migrations when the application starts:

- **Retry logic**: Retries up to `Database:MigrationStartupMaxAttempts` times (default: 40) with a 3-second delay between attempts. This handles cold-start race conditions where the database may not yet be ready.
- **Graceful shutdown**: Respects `CancellationToken` and returns immediately on `OperationCanceledException`.
- **Failure behavior**: Throws `InvalidOperationException` after exhausting all retry attempts.

### 6.2 Registration

```csharp
services.AddHostedService<MigrateOnStartupHostedService>();
```

Registered in `Infrastructure.DependencyInjection.cs:222`.

### 6.3 Configuration

| Key | Default | Description |
|-----|---------|-------------|
| `Database:MigrationStartupMaxAttempts` | `40` | Maximum migration retry attempts |

---

## 7. CI/CD Automation

### 7.1 Build Pipeline

Migrations are validated in CI by:

1. **Compilation check**: The migration code must compile against the current `ECommerceDbContext` model.
2. **Snapshot consistency**: `dotnet ef migrations add --dry-run` verifies the snapshot is up to date.
3. **Test database**: Integration tests apply all migrations against a test PostgreSQL instance.

### 7.2 Deployment Pipeline

```bash
# In the deployment script / Docker entrypoint
dotnet ef database update \
  --project src/ECommerce.Infrastructure \
  --startup-project src/ECommerce.API
```

Alternatively, `MigrateOnStartupHostedService` handles this automatically when the application container starts, eliminating the need for a separate migration job in orchestrated deployments (Kubernetes, Docker Compose).

### 7.3 Best Practices

- **Never skip migrations**: Always apply migrations incrementally. Skipping versions risks schema drift.
- **Idempotent scripts**: Use `--idempotent` when generating deployment scripts for manual application.
- **Separate migration PRs**: Schema changes should be in dedicated PRs reviewed by the team before merging.
- **Back up before production**: Always take a database backup before applying migrations in production.
- **Avoid `EnsureCreated`**: Use `MigrateAsync()` (not `EnsureCreated()`) in production — `EnsureCreated` does not support incremental schema changes.
