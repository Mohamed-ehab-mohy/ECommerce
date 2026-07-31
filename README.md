# ECommerce

Production-scale e-commerce backend built with **.NET 10** and **Clean Architecture**, backed by a one-command local stack
(PostgreSQL, Redis, RabbitMQ, Seq, Prometheus, Grafana) and a green CI pipeline.

## Architecture

- **Domain** — entities, business rules (zero dependencies)
- **UseCases** — application logic, depends only on Domain
- **Infrastructure** — EF Core / Postgres, external integrations
- **API** — ASP.NET Core host (Serilog, OpenTelemetry, health checks, Prometheus metrics)

Layering is enforced by `ECommerce.ArchitectureTests` (NetArchTest) in CI.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/products/docker-desktop/) with Docker Compose v2
- git

## Quick start

```bash
git clone https://github.com/Mohamed-ehab-mohy/ECommerce.git
cd ECommerce

cp .env.example .env        # optional; sane defaults are built in

docker compose up -d        # start the dev stack (Postgres, Redis, RabbitMQ, Seq, Prometheus, Grafana)
dotnet run --project src/ECommerce.API
```

The API listens on <http://localhost:5139>. Verify the stack:

| URL | Purpose |
|-----|---------|
| <http://localhost:5139/health/live> | Liveness probe |
| <http://localhost:5139/health/ready> | Readiness probe (checks Postgres) |
| <http://localhost:5139/metrics> | Prometheus metrics endpoint |
| <http://localhost:5341> | Seq (structured logs + traces) |
| <http://localhost:9090> | Prometheus |
| <http://localhost:3000> | Grafana |

## Dev stack

| Service | Host port | Default credentials |
|---------|-----------|---------------------|
| PostgreSQL 16 | `5433` | `ecommerce` / `ecommerce_dev_pw` |
| Redis 7 | `6379` | — |
| RabbitMQ 3.13 | `5672` (AMQP), `15672` (management UI) | `ecommerce` / `ecommerce_dev_pw` |
| Seq | `5341` | `admin` / `ecommerce_dev_pw` |
| Prometheus | `9090` | — |
| Grafana | `3000` | `admin` / `admin` |

## Configuration

Copy `.env.example` to `.env` and adjust. Never commit `.env`. The Postgres connection string used by the API lives in
`src/ECommerce.API/appsettings.Development.json`.

> Seq: the admin password is fixed at first boot via `SEQ_ADMINPASSWORDHASH` (hash of `ecommerce_dev_pw`). If you change
> `SEQ_PASSWORD`, regenerate the hash (`seqsvr config hash`) and update `SEQ_ADMINPASSWORDHASH`, then wipe the `seq-data`
> volume with `docker compose down -v`.

## Tests

```bash
dotnet build -warnaserror          # builds must be warning-free
dotnet test                        # unit + integration tests
dotnet format --verify-no-changes  # style gate (same as CI)
```

Integration tests spin up real PostgreSQL/Redis/RabbitMQ via Testcontainers and **skip automatically when Docker is
unavailable**.

## Documentation

- `docs/36-developer-onboarding-guide.md` — setup, commands, troubleshooting
- `docs/06-system-architecture.md`, `docs/06a-domain-model.md` — architecture and domain model
- `docs/04-software-requirements-specification.md` — requirements
- `tasks/sprint-01-foundations.md` — sprint backlog

## CI

GitHub Actions (`.github/workflows/ci.yml`) runs build + unit/integration tests + architecture tests, `dotnet format`
verification, and a gitleaks secret scan on every push to `main`.
