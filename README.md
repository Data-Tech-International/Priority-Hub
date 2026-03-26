# Priority Hub

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
- Node.js 20+ and npm (used for repository script wrappers)

Verify your environment:

```bash
dotnet --version
node --version
npm --version
```

## Start The App

1. From repository root, start the app:

```bash
npm run dev
```

2. Alternative direct command:

```bash
dotnet watch --project backend/PriorityHub.Ui/PriorityHub.Ui.csproj run
```

Expected result:

- The Blazor UI starts with hot reload.
- You can open the dashboard and navigate to Settings.

## Configure Connectors

1. Open Settings.
2. Add one or more connector instances.
3. Save configuration.

Expected result:

- Configuration is persisted to `config/providers.local.json`.
- Connectors appear in the dashboard aggregation.

Required fields by provider:

- Azure DevOps: connection name, organization, project, PAT, WIQL.
- Jira: connection name, base URL, email, API token, JQL.
- Trello: connection name, board ID, API key, token.

Notes:

- Client-side validation prevents saving incomplete or invalid forms.
- `config/providers.local.json` is gitignored and must not be committed.

## Build, Test, And Quality Checks

Run from repository root.

Build:

```bash
npm run build
dotnet build PriorityHub.sln
```

Test:

```bash
npm run test
npm run test:api
npm run test:ui

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

- [Features](docs/features/README.md) – dashboard, priority ordering, and multi-connector model.
- [Configuration](docs/configuration/README.md) – provider setup and credential management.
- [Processes](docs/processes/README.md) – versioning, changelog, and contribution workflow.
- [Troubleshooting](docs/troubleshooting/README.md) – common problems and resolution steps.

See [CHANGELOG.md](CHANGELOG.md) for the full version history.

## Project Structure

```text
backend/
  PriorityHub.Api/            # Backend services, models, connectors
  PriorityHub.Api.Tests/      # xUnit backend tests
  PriorityHub.Ui/             # Blazor Server frontend
  PriorityHub.Ui.Tests/       # bUnit component tests
config/                       # Local provider config (gitignored)
docs/                         # User-facing documentation
plans/                        # Specifications and implementation plans
```

## Troubleshooting

Build or SDK mismatch:

- Confirm .NET 10 is installed.
- Re-run `dotnet --version`.

Script command failures:

- Confirm Node.js 20+ and npm are installed.
- Use direct dotnet commands if npm wrappers fail.

No data in dashboard:

- Verify connectors are configured in Settings.
- Check that provider credentials and queries are valid.

Missing local config:

- If `config/providers.local.json` does not exist or is empty, the app still starts and returns an empty dashboard payload so configuration can be completed in the UI.
