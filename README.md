# Priority Hub

Priority Hub is a Vite + React JavaScript application with an ASP.NET Core 10 backend-for-frontend for managing personal priorities across Azure DevOps, Jira, and Trello from a single view.

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

Provider credentials for Azure DevOps, Jira, and Trello do not live in frontend code. The browser calls the ASP.NET Core backend service on `/api/dashboard`, and the backend:

- Stores provider credentials in a local config file managed through the UI.
- Calls each provider API server-side.
- Normalizes external payloads into the shared work item shape.
- Exposes a safe UI-facing API for this React app.

## UI configuration

1. Start the app with `npm.cmd run dev`.
2. Open the configuration studio in the dashboard UI.
3. Add one or more connector instances for Azure DevOps, Jira, and Trello.
4. Save the configuration to persist it into `config/providers.local.json`.

The configuration UI only asks for required fields:

- Azure DevOps: connection name, organization, project, PAT, WIQL.
- Jira: connection name, base URL, email, API token, JQL.
- Trello: connection name, board ID, API key, token.

Client-side validators block save until required fields are present and Jira base URLs and emails are valid.

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

1. Install dependencies with `npm.cmd install`.
2. Start the frontend and backend together with `npm.cmd run dev`.
3. Build the frontend and backend with `npm.cmd run build`.
4. Start only the ASP.NET Core backend watcher with `npm.cmd run dev:server`.
5. Start only the frontend with `npm.cmd run dev:client`.

## Debugging in VS Code

- Use the task `Run Priority Hub` for a full-stack background run.
- Use the launch profile `Priority Hub Full Stack` to start the backend debugger and open the frontend in Edge.

## Code Quality & Testing

### Local Testing

**Frontend:** Run tests with Vitest + React Testing Library
```bash
npm run test                # Run once
npm run test:watch         # Watch mode
npm run test:coverage      # Generate coverage report for core frontend library modules (threshold: 60%)
```

**Backend:** Run tests with xUnit
```bash
dotnet test backend/PriorityHub.Api.Tests/PriorityHub.Api.Tests.csproj
```

### Code Linting

**Frontend:** ESLint enforces React and style rules
```bash
npm run lint       # Check for violations
npm run lint:fix   # Auto-fix style issues
```

**Backend:** dotnet format + StyleCop analyzers
```bash
dotnet format backend/PriorityHub.Api/PriorityHub.Api.csproj  # Auto-format
dotnet build backend/PriorityHub.Api/PriorityHub.Api.csproj /p:EnableNETAnalyzers=true  # Run analyzer checks
```

### CI/CD Pipeline

All code pushed to the `main` branch and PRs automatically run four GitHub workflows:

1. **Coding Standards** — ESLint + dotnet format
   - Enforces consistent code style
   - Fails on readability errors, warns on style violations
   - Can auto-fix many issues with `npm run lint:fix` or `dotnet format`

2. **Security Scanning** — Dependency audit + secret detection
   - Checks npm and NuGet packages for known CVEs
   - Detects accidentally committed secrets (API keys, tokens)
   - Uses TruffleHog + context7 MCP for advisory lookup
   - **Fails on:** Critical/High CVEs, verified secrets
   - **Warns on:** Moderate vulnerabilities

3. **Static Code Analysis** — .NET analyzers + complexity metrics
   - Enforces .NET design patterns and best practices
   - Measures code complexity (max 10 per method)
   - Detects dead code and architectural violations
   - Reports metrics for each build

4. **Test Coverage** — Frontend + backend test execution
   - Runs all unit tests
   - Collects coverage metrics
   - **Fails if:** Frontend core library coverage or backend test run falls below configured standards
   - **Reports:** Coverage delta vs main branch on each PR

**Agent Automation:**  
Each workflow is backed by a specialized agent that:
- Comments on PRs with findings and suggestions
- Creates GitHub issues for critical violations
- Provides auto-fix commands and refactoring hints
- Escalates security findings to team

See [.github/agents/](.github/agents/) for detailed agent behaviors and [.github/MCP-INTEGRATION.md](.github/MCP-INTEGRATION.md) for MCP tool integration.

### Configuration Files

- **`.eslintrc.json`** — Frontend linting rules (max-warnings: 0, strict equality, no console.log)
- **`backend/stylecop.json`** — Backend naming and documentation standards
- **`.editorconfig`** — Cross-editor consistency (indentation, line endings)
- **`.github/pull_request_template.md`** — PR checklist covering tests, security, documentation
- **`vite.config.js`** — Vitest coverage config (60% threshold, v8 provider)

## Current behavior without configuration

If `config/providers.local.json` is missing or empty, the backend still returns a valid dashboard payload with no connections or work items. This lets the UI load cleanly while you configure live provider access from the dashboard itself.