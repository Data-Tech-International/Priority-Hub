# Priority Hub

Priority Hub is a Blazor Server application backed by ASP.NET Core 10 for managing personal priorities across Azure DevOps, Jira, and Trello from a single view.

## What is included

- A unified work item model that normalizes source data from multiple board systems.
- A ranked queue that scores work using impact, urgency, confidence, blockers, age, and estimated effort.
- A dashboard UI with connector health, filtering, and a single personal priority view.
- An ASP.NET Core backend-for-frontend that queries Azure DevOps with WIQL, Jira with JQL, and Trello board APIs using server-side credentials.
- A UI-based configuration studio that can create and edit multiple connector instances for each provider type.
- Client-side validation for connector setup before configuration is saved.
- Support for multiple Azure DevOps projects at the same time, plus multiple Jira and Trello connector instances in parallel.
- A drag-drop personal order that persists across items from all providers.
- Visual highlighting for newly retrieved items until you place them into your own order.

## Runtime architecture

The Blazor Server UI runs on the same ASP.NET Core host as the backend services. Components inject connector services directly through dependency injection — there is no separate HTTP API boundary between UI and backend logic.

Provider credentials for Azure DevOps, Jira, and Trello do not live in frontend code. The host process:

- Stores provider credentials in a local config file managed through the UI.
- Calls each provider API server-side.
- Normalizes external payloads into the shared work item shape.
- Streams results to the browser over a persistent SignalR connection.

## UI configuration

1. Start the app with `npm run dev` (or `dotnet watch --project backend/PriorityHub.Ui`).
2. Open the configuration studio in the Settings page.
3. Add one or more connector instances for Azure DevOps, Jira, and Trello.
4. Save the configuration to persist it into `config/providers.local.json`.

The configuration UI only asks for required fields:

- Azure DevOps: connection name, organization, project, PAT, WIQL.
- Jira: connection name, base URL, email, API token, JQL.
- Trello: connection name, board ID, API key, token.

Validators block save until required fields are present and Jira base URLs and emails are valid.

`config/providers.local.json` is gitignored and is intended for local secrets only.

## Supported connection model

- Azure DevOps connections accept connection name, organization, project, PAT, and WIQL.
- Jira connections accept base URL, email, API token, and JQL.
- Trello connections accept connection name, board ID, API key, and token.
- Each provider can define multiple connections, so the dashboard can aggregate several boards per system.
- Multiple Azure DevOps connections can point to different projects in the same organization or across different organizations and run at the same time.

## Personal ordering

- The unified work queue is manually reorderable with drag and drop.
- Order is persisted on the backend across items from all providers.
- Newly retrieved items are marked as new, highlighted visually, and floated to the top until you place them into your own order.

## Local development

### Prerequisites

- .NET 10 SDK
- No Node.js runtime required for the app itself (npm scripts are convenience wrappers for dotnet commands)

### Quick start

```bash
# Start the Blazor Server app with hot reload
npm run dev

# Or use dotnet directly
dotnet watch --project backend/PriorityHub.Ui/PriorityHub.Ui.csproj run
```

### Build

```bash
npm run build          # Build entire solution
dotnet build PriorityHub.sln
```

### Project structure

```
backend/
  PriorityHub.Api/            # Shared backend services, models, and connectors
  PriorityHub.Api.Tests/      # xUnit backend tests
  PriorityHub.Ui/             # Blazor Server frontend
    Components/               # Razor components (NavBar, HelpPanel, TagFilter)
      Layout/                 # MainLayout and reconnect modal
      Pages/                  # Route-level pages (Dashboard, Settings, Login)
    Services/                 # UI-specific services (WorkItemRanker, HelpContent)
    wwwroot/                  # Static assets (CSS, fonts, JS interop)
  PriorityHub.Ui.Tests/       # bUnit component tests
config/                       # Local provider config (gitignored)
plans/                        # Specifications and implementation plans
```

## Debugging in VS Code

- Use the task **Run Priority Hub** for a `dotnet watch` background run.
- Use the launch profile **Priority Hub** to start the Blazor Server debugger and auto-open in your browser.
- Use the launch profile **Priority Hub Backend (API only)** to debug the API project standalone.

## Code Quality & Testing

### Local Testing

**Backend:** Run all tests with xUnit + bUnit
```bash
npm run test            # Run all tests (API + UI)
npm run test:api        # API tests only
npm run test:ui         # Blazor component tests only

# Or use dotnet directly
dotnet test PriorityHub.sln
dotnet test backend/PriorityHub.Api.Tests/PriorityHub.Api.Tests.csproj
dotnet test backend/PriorityHub.Ui.Tests/PriorityHub.Ui.Tests.csproj
```

### Code Linting

**Backend:** dotnet format + StyleCop analyzers
```bash
dotnet format PriorityHub.sln                                          # Auto-format
dotnet build PriorityHub.sln /p:EnableNETAnalyzers=true                # Run analyzer checks
```

### CI/CD Pipeline

All code pushed to the `main` branch and PRs automatically run four GitHub workflows:

1. **Coding Standards** — dotnet format
   - Enforces consistent code style
   - Fails on readability errors, warns on style violations

2. **Security Scanning** — Dependency audit + secret detection
   - Checks NuGet packages for known CVEs
   - Detects accidentally committed secrets (API keys, tokens)
   - Uses TruffleHog for secret scanning
   - **Fails on:** Critical/High CVEs, verified secrets
   - **Warns on:** Moderate vulnerabilities

3. **Static Code Analysis** — .NET analyzers + complexity metrics
   - Enforces .NET design patterns and best practices
   - Measures code complexity (max 10 per method)
   - Detects dead code and architectural violations

4. **Test Coverage** — Backend + Blazor component test execution
   - Runs all unit tests
   - Collects coverage metrics
   - **Fails if:** Test coverage falls below configured standards

**Agent Automation:**  
Each workflow is backed by a specialized agent that:
- Comments on PRs with findings and suggestions
- Creates GitHub issues for critical violations
- Provides auto-fix commands and refactoring hints
- Escalates security findings to team

See [.github/agents/](.github/agents/) for detailed agent behaviors and [.github/MCP-INTEGRATION.md](.github/MCP-INTEGRATION.md) for MCP tool integration.

### Specification-First Agent Workflow

Major changes follow a strict lifecycle so implementation always starts from a specification:

1. **Draft spec** — Create a GitHub issue using the `Specification Request` template (label: `specification`). You can draft specs interactively using VS Code Copilot Chat Plan mode.
2. **Capture** — Workflow `spec-intake.yml` persists the issue as markdown in `plans/specifications/` and adds label `needs-plan`.
3. **Plan** — Workflow `spec-plan.yml` generates an implementation plan in `plans/` and labels the issue `plan-proposed`.
4. **Review** — Review the plan in VS Code Copilot Chat or on GitHub. When satisfied, add label `plan-approved`.
5. **Bootstrap** — Workflow `spec-implementation-bootstrap.yml` automatically creates:
   - an implementation issue (assigned to spec author + `copilot`),
   - a feature branch from `main`,
   - and a draft PR targeting `main`.
6. **Implement** — GitHub Copilot Coding Agent picks up the implementation issue, reads `AGENTS.md` for conventions, and implements code, tests, and documentation on the feature branch.
7. **Review PR** — Review the draft PR in VS Code or on GitHub. Use `/review` for self-check before merging. CI workflows validate linting, security, and test coverage automatically.

```
YOU (VS Code)                    GITHUB AUTOMATION               COPILOT CODING AGENT
───────────────                  ─────────────────               ────────────────────
1. Draft specification        →  2. Capture spec file
                                 3. Propose plan
4. Review & approve plan      →  5. Create issue + branch + PR
                                                              →  6. Implement code/tests/docs
7. Review & merge PR          ←                               ←  PR ready for review
```

**Key files:**
- `AGENTS.md` — Instructions for GitHub Copilot Coding Agent
- `.github/copilot-setup-steps.yml` — Agent environment setup (Node 20, .NET 10)
- `.github/agents/*.agent.md` — VS Code Copilot Chat agent definitions

This process ensures traceability from specification to plan to implementation and keeps planning artifacts in the repository.

### Configuration Files

- **`backend/stylecop.json`** — Backend naming and documentation standards
- **`.editorconfig`** — Cross-editor consistency (indentation, line endings)
- **`.github/pull_request_template.md`** — PR checklist covering tests, security, documentation

## Current behavior without configuration

If `config/providers.local.json` is missing or empty, the backend still returns a valid dashboard payload with no connections or work items. This lets the UI load cleanly while you configure live provider access from the dashboard itself.

## Future roadmap

To start work on a planned change, follow the specification-first workflow described in the **Code Quality & Testing** section above.