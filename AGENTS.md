# Priority Hub — Coding Agent Instructions

You are a coding agent working on Priority Hub, a Vite + React JavaScript frontend with an ASP.NET Core 10 backend-for-frontend that unifies Azure DevOps, Jira, and Trello work items into one personal priority dashboard.

## Project Structure

```
src/                          # React frontend (Vite, JSX)
  components/                 # Reusable UI components
  pages/                      # Route-level page components
  lib/                        # Utilities, API client, auth, caching
  contexts/                   # React context providers
  test/                       # Test setup
backend/
  PriorityHub.Api/            # ASP.NET Core backend (C#, .NET 10)
    Services/                 # Business logic and connectors
    Models/                   # Shared data models
  PriorityHub.Api.Tests/      # xUnit backend tests
config/                       # Local provider config (gitignored)
plans/                        # Specifications and implementation plans
```

## Implementation Rules

When assigned an implementation issue, follow these steps in order:

1. **Read the specification and plan.** The issue body links to a spec issue and a plan file in `plans/`. Read both fully before writing any code.
2. **Create focused commits.** Each commit should address one logical change. Use conventional commit messages referencing the issue: `feat(#42): add input validation`.
3. **Write production code** in the appropriate directories. Frontend code goes in `src/`, backend code in `backend/PriorityHub.Api/`.
4. **Write or update tests** for every behavior change:
   - Frontend: Vitest + React Testing Library in `src/**/*.test.js`
   - Backend: xUnit in `backend/PriorityHub.Api.Tests/`
5. **Update documentation:** Update `README.md` if setup or usage changes. Update the plan file to mark completed steps.
6. **Run verification commands** before marking work complete:
   - `npm run lint` — frontend linting
   - `npm run test` — frontend tests
   - `npm run test:coverage` — coverage must stay above 60%
   - `dotnet build backend/PriorityHub.Api/PriorityHub.Api.csproj` — backend compilation
   - `dotnet test backend/PriorityHub.Api.Tests/PriorityHub.Api.Tests.csproj` — backend tests

## Code Conventions

### Frontend (JavaScript/React)
- 2-space indentation, single quotes, semicolons required
- Strict equality (`===`) only
- No `console.log()` — use `console.error()` or `console.warn()` only
- Components in `src/components/`, pages in `src/pages/`, utilities in `src/lib/`
- No direct API calls in components — use `src/lib/api.js`

### Backend (C#/.NET)
- 4-space indentation, PascalCase public members, `_camelCase` private fields
- Public classes and methods must have XML documentation
- Services injected via dependency injection
- No hardcoded configuration or secrets — use `config/providers.local.json` (gitignored)
- All external HTTP calls go through connector services in `Services/Connectors/`

## Security Rules

- Never hardcode secrets, API keys, or tokens in source code
- Never commit `config/providers.local.json` — it is gitignored
- Validate all external input at API boundaries
- Use parameterized queries and safe string handling

## What NOT To Do

- Do not modify files outside the scope of the implementation issue
- Do not remove or weaken existing tests
- Do not change CI workflow files unless the plan explicitly requires it
- Do not push to `main` directly — all work goes through the feature branch and PR
