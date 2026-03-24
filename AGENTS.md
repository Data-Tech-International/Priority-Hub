# Priority Hub — Coding Agent Instructions

You are a coding agent working on Priority Hub, a Blazor Server frontend with an ASP.NET Core 10 backend-for-frontend that unifies Azure DevOps, Jira, and Trello work items into one personal priority dashboard.

## Project Structure

```
backend/
  PriorityHub.Api/            # ASP.NET Core backend (C#, .NET 10)
    Services/                 # Business logic and connectors
    Models/                   # Shared data models
  PriorityHub.Api.Tests/      # xUnit backend tests
  PriorityHub.Ui/             # Blazor Server frontend (C#, .NET 10)
    Components/               # Razor components (NavBar, HelpPanel, TagFilter)
      Layout/                 # MainLayout, reconnect modal
      Pages/                  # Route-level pages (Dashboard, Settings, Login)
    Services/                 # UI-specific services (WorkItemRanker, HelpContent)
    wwwroot/                  # Static assets (CSS, fonts, JS interop)
  PriorityHub.Ui.Tests/       # bUnit component tests
config/                       # Local provider config (gitignored)
plans/                        # Specifications and implementation plans
```

## Implementation Rules

When assigned an implementation issue, follow these steps in order:

1. **Read the specification and plan.** The issue body links to a spec issue and a plan file in `plans/`. Read both fully before writing any code.
2. **Create focused commits.** Each commit should address one logical change. Use conventional commit messages referencing the issue: `feat(#42): add input validation`.
3. **Write production code** in the appropriate directories. Shared backend services go in `backend/PriorityHub.Api/`, Blazor UI code in `backend/PriorityHub.Ui/`.
4. **Write or update tests** for every behavior change:
   - UI components: bUnit in `backend/PriorityHub.Ui.Tests/`
   - Backend services: xUnit in `backend/PriorityHub.Api.Tests/`
5. **Update documentation:** Update `README.md` if setup or usage changes. Update the plan file to mark completed steps.
6. **Run verification commands** before marking work complete:
   - `dotnet build PriorityHub.sln` — full solution compilation
   - `dotnet test PriorityHub.sln` — all tests (API + UI)

## Code Conventions

### Frontend (Blazor/Razor)
- 4-space indentation, PascalCase for component parameters and public members
- Components in `backend/PriorityHub.Ui/Components/`, pages in `Components/Pages/`
- UI-specific services in `backend/PriorityHub.Ui/Services/`
- Use dependency injection for all service access — no direct HTTP calls from components
- JS interop only when Blazor has no native equivalent (e.g., localStorage, outside-click detection)

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
