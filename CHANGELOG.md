# Changelog

All notable changes to Priority Hub are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Priority Hub adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `IConfigStore` interface abstracting per-user configuration persistence (`PriorityHub.Api/Services/IConfigStore.cs`).
- `PostgresConfigStore` storing configuration as JSONB with a monotonically increasing `version` column for write auditing (`PriorityHub.Api/Services/PostgresConfigStore.cs`).
- `SchemaManager` that auto-applies SQL migrations embedded in the assembly; auto-runs in Development and fails fast on mismatch in other environments (`PriorityHub.Api/Data/SchemaManager.cs`).
- SQL migration `0001_initial_schema.sql` creating the `schema_migrations` and `user_config` tables.
- `ConfigStoreServiceExtensions.AddConfigStore` selects `Postgres` or `File` provider based on `ConfigStore:Provider` configuration key (`PriorityHub.Api/Extensions/ConfigStoreServiceExtensions.cs`).
- `ConfigStore:Provider` and `ConfigStore:ConnectionString` configuration keys in `appsettings.json` (default: `File`).
- `appsettings.Development.json` in both `PriorityHub.Ui` and `PriorityHub.Api` defaulting to `Postgres` with local container credentials.
- `docker-compose.yml` at the repository root providing a one-command PostgreSQL 16 container for local development.
- Integration tests for `PostgresConfigStore` and `SchemaManager` using ephemeral `Testcontainers.PostgreSql` containers (`PriorityHub.Api.Tests/PostgresConfigStoreIntegrationTests.cs`).
- Contributing guide, issue templates, CI badges, docs skeleton, semantic versioning governance.
- Multi-stage `Dockerfile` at the repository root targeting `backend/PriorityHub.Ui/PriorityHub.Ui.csproj`; uses .NET 10 SDK for build and ASP.NET Core 10 runtime for the final image; exposes port `8080`.
- `.dockerignore` excluding build outputs, secrets, editor caches, and local config from the Docker build context.
- GitHub Actions workflow `.github/workflows/docker-image.yml` — builds on every pull request to `main`, builds and publishes to GHCR on push to `main`, and supports `workflow_dispatch` on any branch; adds OCI labels and commit-SHA/branch tags; cancels superseded runs per ref.

### Changed
- `LocalConfigStore` now implements `IConfigStore`.
- `DashboardAggregator` depends on `IConfigStore` instead of the concrete `LocalConfigStore`.
- Both `Program.cs` files register `IConfigStore` via `AddConfigStore` and run `ApplyDatabaseMigrationsAsync` before `app.Run()`.
- README updated with Docker, local database bootstrap, and project structure changes.
- README added Container Quickstart, deployment configuration guidance, and container troubleshooting.
- `docs/processes/README.md` updated with Docker image lifecycle section.
- `docs/troubleshooting/README.md` updated with container-specific troubleshooting entries.

## [0.2.0] - 2025-01-01

### Added
- Multi-connector model supporting multiple Azure DevOps, Jira, and Trello instances in parallel.
- Manual drag-and-drop ordering persisted across all sources.
- New-item highlighting until items are placed in the preferred order.
- Blazor Server UI with ASP.NET Core 10 backend-for-frontend (BFF).
- Normalized work item model across all providers.
- Ranked priority queue using impact, urgency, confidence, blockers, age, and effort.
- Dashboard with connector health indicators, filtering, and a single personal priority view.
- Settings UI for managing provider credentials stored in `config/providers.local.json`.
- Specification-first delivery workflow with plan files in `plans/`.

[Unreleased]: https://github.com/Data-Tech-International/Priority-Hub/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/Data-Tech-International/Priority-Hub/releases/tag/v0.2.0
