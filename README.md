# Priority Hub

[![CI](https://github.com/Data-Tech-International/Priority-Hub/actions/workflows/ci.yml/badge.svg)](https://github.com/Data-Tech-International/Priority-Hub/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Priority Hub is a Blazor Server application with an ASP.NET Core 10 backend-for-frontend (BFF) that unifies personal work items from Azure DevOps, Jira, and Trello.

## Overview

Priority Hub provides:

- A normalized work item model across providers.
- A ranked queue using impact, urgency, confidence, blockers, age, and effort.
- A dashboard with connector health, filtering, and a single personal priority view.
- A multi-connector model (multiple Azure DevOps, Jira, and Trello instances in parallel).
- Manual drag-and-drop ordering persisted across all sources.
- New-item highlighting until items are placed in your preferred order.

## Architecture

The Blazor Server UI and ASP.NET Core services run in the same host process.

- UI components consume backend services through dependency injection.
- There is no separate HTTP boundary between UI and backend logic.
- Provider credentials are stored locally and used only server-side.
- Backend connectors call provider APIs and normalize payloads before streaming updates to the browser through SignalR.

## Prerequisites

- .NET 10 SDK
- Docker (for the local PostgreSQL container)

Verify your environment:

```bash
dotnet --version
docker --version
```

## Local Database Setup

Priority Hub uses PostgreSQL (JSONB) to store per-user configuration in Development.
A pre-configured Docker Compose file is included for a zero-friction local setup.

### Start PostgreSQL

```bash
docker compose up -d
```

This starts a PostgreSQL 16 container on port `5432` with:

| Setting    | Value          |
|------------|----------------|
| Host       | `localhost`    |
| Port       | `5432`         |
| Database   | `priorityhub`  |
| Username   | `priorityhub`  |
| Password   | `dev_password` |

The application auto-runs any pending schema migrations on startup in Development,
so no manual `CREATE TABLE` steps are required.

### One-time local configuration

`appsettings.Development.json` is gitignored (it may contain local secrets such as OAuth app credentials).
Copy the included example file once after a fresh clone:

```bash
cp backend/PriorityHub.Ui/appsettings.Development.example.json \
   backend/PriorityHub.Ui/appsettings.Development.json
```

This activates the Postgres provider with the default docker-compose credentials.
If you need to point to a different database or add OAuth secrets, edit the file locally.

### Stop and Wipe Data

```bash
# Stop and remove containers (data volume is preserved)
docker compose down

# Stop and remove containers AND the data volume (full reset)
docker compose down -v
```

### Using File-Based Storage Instead

If you prefer to skip Docker, you can revert to the legacy file-based store by
overriding the `ConfigStore:Provider` value in your local user secrets or
`appsettings.Development.json`:

```json
{
  "ConfigStore": {
    "Provider": "File"
  }
}
```

## Start The App

1. Complete the [Local Database Setup](#local-database-setup) steps if you haven't already (one-time).

2. From repository root, start the app:

```bash
dotnet watch --project backend/PriorityHub.Ui/PriorityHub.Ui.csproj run
```

Expected result:

- The Blazor UI starts with hot reload.
- You can open the dashboard and navigate to Settings.
- Schema migrations are applied automatically on the first run.

## Configure Connectors

1. Open Settings.
2. Add one or more connector instances.
3. Save configuration.

Expected result:

- Configuration is persisted to PostgreSQL (or `config/providers.local.json` when using file store).
- Connectors appear in the dashboard aggregation.

Required fields by provider:

- Azure DevOps: connection name, organization, project, PAT, WIQL.
- Jira: connection name, base URL, email, API token, JQL.
- Trello: connection name, board ID, API key, token.

Notes:

- Client-side validation prevents saving incomplete or invalid forms.
- `config/providers.local.json` is gitignored and must not be committed.

## Container Quickstart

Priority Hub ships with a production-ready multi-stage Dockerfile. The container exposes port `8080` and defaults to `ASPNETCORE_ENVIRONMENT=Production`.

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/)
- A PostgreSQL instance (see [Local PostgreSQL via Docker](#local-postgresql-via-docker))
- OAuth app credentials (GitHub and/or Microsoft)

### Build the image

```bash
docker build -t priority-hub:local .
```

### Local PostgreSQL via Docker

```bash
docker compose up -d
```

This starts a PostgreSQL 16 container on `localhost:5432` with the credentials shown in [Local Database Setup](#local-database-setup).

### Run the container

On **Linux** (using host networking so the container can reach the local PostgreSQL):

```bash
docker run --rm -it \
  --network host \
  -e ConfigStore__Provider=Postgres \
  -e ConfigStore__ConnectionString="Host=localhost;Database=priorityhub;Username=priorityhub;Password=dev_password" \
  -e Authentication__GitHub__ClientId=<your-github-client-id> \
  -e Authentication__GitHub__ClientSecret=<your-github-client-secret> \
  priority-hub:local
```

On **macOS / Windows** (Docker Desktop does not support `--network host`; use `host.docker.internal` instead):

```bash
docker run --rm -it \
  -e ConfigStore__Provider=Postgres \
  -e ConfigStore__ConnectionString="Host=host.docker.internal;Database=priorityhub;Username=priorityhub;Password=dev_password" \
  -e Authentication__GitHub__ClientId=<your-github-client-id> \
  -e Authentication__GitHub__ClientSecret=<your-github-client-secret> \
  -p 8080:8080 \
  priority-hub:local
```

Replace `<your-github-client-id>` and `<your-github-client-secret>` with your OAuth app credentials.
See [docs/configuration/README.md](docs/configuration/README.md) for the full environment variable reference.

Expected result:

- Container starts and logs `Application started. Press Ctrl+C to shut down.`
- Health check responds: `curl http://localhost:8080/api/health`

### Verify startup

```bash
# Check the health endpoint
curl http://localhost:8080/api/health
# Expected: {"ok":true,"generatedAt":"..."}

# Inspect container logs
docker logs <container-id>
```

### Using a published image from GHCR

Images are published to GitHub Container Registry on every push to `main`.

```bash
# Log in to GHCR (one-time)
echo $GITHUB_TOKEN | docker login ghcr.io -u <your-github-username> --password-stdin

# Pull the latest image
docker pull ghcr.io/data-tech-international/priority-hub:latest

# Run the published image
docker run --rm -it \
  -e ConfigStore__Provider=Postgres \
  -e ConfigStore__ConnectionString="Host=<db-host>;Database=priorityhub;Username=priorityhub;Password=<password>" \
  -e Authentication__GitHub__ClientId=<your-client-id> \
  -e Authentication__GitHub__ClientSecret=<your-client-secret> \
  -p 8080:8080 \
  ghcr.io/data-tech-international/priority-hub:latest
```

### Roll back to a prior image

Each published image is tagged with `sha-<short-commit-sha>` for traceability.

```bash
# List available tags
docker pull ghcr.io/data-tech-international/priority-hub:<sha-tag>

# Run a specific version
docker run --rm -it \
  -e ConfigStore__Provider=Postgres \
  -e ConfigStore__ConnectionString="..." \
  -p 8080:8080 \
  ghcr.io/data-tech-international/priority-hub:sha-abc1234
```

### Deployment configuration

| Environment variable | Required | Description |
|---|---|---|
| `ConfigStore__Provider` | ✔ | Set to `Postgres` for containerized deployments |
| `ConfigStore__ConnectionString` | ✔ (when Postgres) | PostgreSQL connection string |
| `Authentication__GitHub__ClientId` | ✔ (if using GitHub login) | GitHub OAuth app client ID |
| `Authentication__GitHub__ClientSecret` | ✔ (if using GitHub login) | GitHub OAuth app client secret |
| `Authentication__Microsoft__ClientId` | ✔ (if using Microsoft login) | Microsoft OAuth app client ID |
| `Authentication__Microsoft__ClientSecret` | ✔ (if using Microsoft login) | Microsoft OAuth app client secret |
| `Authentication__Microsoft__TenantId` | optional | Microsoft tenant ID (default: `common`) |
| `ASPNETCORE_URLS` | optional | Override listen address (default: `http://+:8080`) |

**Reverse proxy and HTTPS:** Run the container behind a reverse proxy (nginx, Caddy, Traefik) that handles TLS termination. The proxy must forward `X-Forwarded-For`, `X-Forwarded-Proto`, and `X-Forwarded-Host` headers so ASP.NET Core can reconstruct the correct public URL for OAuth callbacks. Enable forwarded-headers middleware in the application if deploying behind a proxy (the `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` environment variable enables this for ASP.NET Core).

**Sticky sessions:** Blazor Server uses persistent SignalR connections. When running multiple container replicas, configure session affinity (sticky sessions) at your load balancer so each browser session is always routed to the same replica.

**Database migrations:** In `Production` the application validates that all migrations have been applied and will fail fast if any are pending. Apply migrations manually before deploying a new image version:

```bash
# Using psql directly
psql -h <db-host> -U priorityhub -d priorityhub -f backend/PriorityHub.Api/Data/Migrations/0001_initial_schema.sql
```

**Database backups:** Back up your PostgreSQL database before deploying a new image that includes schema migrations.

## Azure Production Deployment

Production deployment to Azure is managed in a separate private repository ([Priority-Hub-DTI](https://github.com/Data-Tech-International/Priority-Hub-DTI)). The `docker-image.yml` workflow in this repo automatically triggers the deployment pipeline in Priority-Hub-DTI after pushing a new image to GHCR on main.

## Build, Test, And Quality Checks

Run from repository root.

Build:

```bash
dotnet build PriorityHub.sln
```

Test:

```bash
dotnet test PriorityHub.sln
dotnet test backend/PriorityHub.Api.Tests/PriorityHub.Api.Tests.csproj
dotnet test backend/PriorityHub.Ui.Tests/PriorityHub.Ui.Tests.csproj
```

Formatting and analyzer checks:

```bash
dotnet format PriorityHub.sln
dotnet build PriorityHub.sln /p:EnableNETAnalyzers=true
```

Expected result:

- Build completes without errors.
- Tests pass.
- Formatting and analyzer checks report no blocking issues.

## Debug In VS Code

- Run task: Run Priority Hub (background `dotnet watch` run).
- Launch profile: Priority Hub (Blazor UI debugging with browser launch).
- Launch profile: Priority Hub Backend (API only).

## CI Workflows

Pull requests and main-branch updates run:

1. Coding standards checks.
2. Security scanning (dependencies and secret detection).
3. Static analysis.
4. Test and coverage validation.
5. Docker image build (PR to `main`: build only; push to `main`: build and publish to GHCR).

For agent and workflow details, see [.github/agents/](.github/agents/) and [.github/MCP-INTEGRATION.md](.github/MCP-INTEGRATION.md).

## Specification-First Workflow

Major changes follow specification-first delivery:

1. Create a specification issue (label: `specification`).
2. Generate a plan file in `plans/`.
3. Review and approve plan (label: `plan-approved`).
4. Implement on the generated feature branch and draft pull request.
5. Validate with build, test, and documentation updates.

Key references:

- `AGENTS.md` for coding-agent rules.
- `.github/copilot-setup-steps.yml` for agent environment setup.
- `plans/` for approved implementation plans.

## Documentation

Full user documentation is in [`docs/`](docs/):

- [Quick Start](docs/quick-start.md) – sign in, add connectors, and start using the dashboard.
- [Features](docs/features/README.md) – dashboard, priority ordering, and multi-connector model.
- [Configuration](docs/configuration/README.md) – provider setup and credential management.
- [Processes](docs/processes/README.md) – versioning, changelog, and contribution workflow.
- [Troubleshooting](docs/troubleshooting/README.md) – common problems and resolution steps.

See [CHANGELOG.md](CHANGELOG.md) for the full version history.

## Project Structure

```text
backend/
  PriorityHub.Api/            # Backend services, models, connectors
    Data/Migrations/          # SQL schema migration scripts
  PriorityHub.Api.Tests/      # xUnit backend tests (unit + integration)
  PriorityHub.Ui/             # Blazor Server frontend
  PriorityHub.Ui.Tests/       # bUnit component tests
config/                       # Local provider config (gitignored)
docker-compose.yml            # Local PostgreSQL container
Dockerfile                    # Multi-stage production container build
.dockerignore                 # Files excluded from Docker build context
docs/                         # User-facing documentation
plans/                        # Specifications and implementation plans
```

## Troubleshooting

Build or SDK mismatch:

- Confirm .NET 10 is installed.
- Re-run `dotnet --version`.

Script command failures:

- Confirm .NET 10 SDK is installed.
- Re-run `dotnet --version`.

No data in dashboard:

- Verify connectors are configured in Settings.
- Check that provider credentials and queries are valid.

Missing local config:

- If `config/providers.local.json` does not exist or is empty, the app still starts and returns an empty dashboard payload so configuration can be completed in the UI.

Container startup crash:

- Check for missing required environment variables (`ConfigStore__Provider`, `ConfigStore__ConnectionString`).
- Confirm PostgreSQL is reachable from the container (`docker logs <container-id>`).
- Ensure schema migrations have been applied for non-Development environments.

OAuth callback URL mismatch in a container:

- Register `http(s)://<your-host>/api/auth/callback/github` and `/api/auth/callback/microsoft` in your OAuth app settings.

See [docs/troubleshooting/README.md](docs/troubleshooting/README.md) for detailed guidance.

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on
reporting bugs, requesting features, setting up your development environment, coding standards,
and the pull request process.

For major changes, follow the [specification-first workflow](CONTRIBUTING.md#specification-first-workflow-major-changes)
before starting implementation.

## Security

If you discover a security vulnerability, please **do not** open a public issue.
Report it privately following the process described in [SECURITY.md](SECURITY.md).

## License

Priority Hub is released under the [MIT License](LICENSE).
