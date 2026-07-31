# Developer Onboarding Guide

This guide takes a new developer from a clean clone to a running API with a healthy stack in under 30 minutes.

## 1. Setup

1. Install the **.NET 10 SDK** — <https://dotnet.microsoft.com/download/dotnet/10.0>.
   Verify: `dotnet --version`
2. Install **Docker Desktop** and start the Docker engine.
   Verify: `docker info`
3. Clone the repository:

   ```bash
   git clone https://github.com/Mohamed-ehab-mohy/ECommerce.git
   cd ECommerce
   ```

4. (Optional) Create local configuration overrides:

   ```bash
   cp .env.example .env
   ```

   The `.env` file feeds `docker compose`; defaults are fine for local development. Never commit `.env`.

5. Start the dev stack:

   ```bash
   docker compose up -d
   docker compose ps   # all services should be running/healthy
   ```

6. Run the API:

   ```bash
   dotnet run --project src/ECommerce.API
   ```

7. Verify:

   - <http://localhost:5139/health/live> returns `200` (liveness)
   - <http://localhost:5139/health/ready> returns `200` (readiness incl. Postgres)
   - <http://localhost:5341> — Seq: logs + traces, login `admin` / `ecommerce_dev_pw`
   - <http://localhost:3000> — Grafana (admin / admin)
   - <http://localhost:9090> — Prometheus, target `ecommerce-api` should be `UP`

## 2. Common commands

| Task | Command |
|------|---------|
| Build (warning-free) | `dotnet build -warnaserror` |
| Run the API | `dotnet run --project src/ECommerce.API` |
| Run all tests | `dotnet test` |
| Run only unit tests | `dotnet test tests/ECommerce.UnitTests` |
| Style check | `dotnet format --verify-no-changes` |
| Start/stop stack | `docker compose up -d` / `docker compose down` |
| Reset stack + volumes | `docker compose down -v` |
| Add an EF Core migration | `dotnet ef migrations add <Name> --project src/ECommerce.Infrastructure` |
| Apply migrations manually | `dotnet ef database update --project src/ECommerce.Infrastructure` |

EF Core uses the design-time factory in `src/ECommerce.Infrastructure/Data/ECommerceDbContextFactory.cs`; override the
connection string with the `ConnectionStrings__Postgres` environment variable if needed.

## 3. Tests

- **Unit tests** (`tests/ECommerce.UnitTests`) — fast, no infrastructure.
- **Integration tests** (`tests/ECommerce.IntegrationTests`) — use Testcontainers to run real
  PostgreSQL/Redis/RabbitMQ; they are skipped automatically when Docker is not available.
- **Architecture tests** (`tests/ECommerce.ArchitectureTests`) — enforce Clean Architecture dependencies; part of CI.

## 4. Troubleshooting

| Symptom | Fix |
|---------|-----|
| `docker compose ps` shows unhealthy Postgres | The container is still starting; wait and re-check. If the host port `5433` is taken, change the mapping in `docker-compose.yml`. |
| `/health/ready` returns `503` | Postgres is not reachable — confirm `docker compose ps` shows postgres healthy and the connection string in `src/ECommerce.API/appsettings.Development.json` matches. |
| Seq asks to change the admin password or login fails | The admin password is set once on first boot from `SEQ_ADMINPASSWORDHASH` (a hash of `ecommerce_dev_pw`). If you changed `SEQ_PASSWORD`, regenerate the hash with `seqsvr config hash`, update `SEQ_ADMINPASSWORDHASH` in `.env`, and reset the stack with `docker compose down -v && docker compose up -d`. |
| Integration tests are skipped | Docker is not running — start Docker Desktop and re-run `dotnet test`. |
| `dotnet ef` command not found | Install the global tool: `dotnet tool install --global dotnet-ef`. |
| Restore/build errors mentioning NuGet sources | Confirm you are behind the repo's required NuGet source(s); no private feed is used by CI. |
| Port already in use on `5139` | Stop the other process or change `applicationUrl` in `src/ECommerce.API/Properties/launchSettings.json`. |

## 5. Conventions

- Clean Architecture: **Domain → UseCases → Infrastructure → API**; dependencies point inward only.
- Warnings are treated as errors; `dotnet format` must pass.
- Do not commit secrets or `.env`; the gitleaks secret scan in CI will fail the build otherwise.
