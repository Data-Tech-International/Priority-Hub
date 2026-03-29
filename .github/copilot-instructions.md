# Copilot Instructions - Priority Hub

This file is the workspace-level quick guide for AI agents. Detailed coding rules live in `AGENTS.md`.

## 1) Must Follow

### Workflow
- Use spec-first delivery:
	1. Specification issue labeled `specification`
	2. Plan file created in `plans/`
	3. Implementation starts only after `plan-approved`
- Ask clarifying questions when requirements are ambiguous.
- Keep implementation aligned with approved scope.

### Verification
- Validate every completed change with:
	- `dotnet build PriorityHub.sln`
	- `dotnet test PriorityHub.sln`

### Security
- Never commit secrets or local sensitive config.
- Keep `config/providers.local.json` out of version control.

### Versioning and Changelog (Mandatory)
- Follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html): `MAJOR.MINOR.PATCH`.
- `backend/Directory.Build.props` is the single source of truth for the project version. Managed by release-please.
- Every behavior change requires an entry under `[Unreleased]` in `CHANGELOG.md` (Keep a Changelog format).
- See `AGENTS.md` for complete versioning and changelog rules.

### Documentation
- Keep `README.md` as the entry point for setup, usage, and links to `docs/`.
- Keep detailed user documentation in `docs/` (features, configuration, processes, troubleshooting).
- Keep plan files in `plans/` updated with progress and verification steps.

### Documentation Environment Setup
- Validate environment before doc updates:
	- .NET 10 available
- Re-run and confirm documented commands still work:
	- `dotnet build PriorityHub.sln`
	- `dotnet test PriorityHub.sln`
	- app run command documented in `README.md`
- Keep documentation updates in the same change as behavior changes.

## 2) Recommended

### Collaboration and Quality
- Use VS Code Copilot Plan mode to draft/review specs and plans.
- Run `/review` on PRs before human review.
- Keep commits focused and history clean.
- Archive or remove outdated plans and docs.

### Technical Documentation Style
- Write task-first user instructions: prerequisites, steps, expected outcome.
- Keep command examples copy-paste ready and repository-accurate.
- Add concise troubleshooting for common errors.
- Keep naming and terminology consistent with code and UI.

### Architecture Guidance
- Preserve Blazor Server + ASP.NET Core BFF architecture.
- Keep DI-first integration between UI and backend services.
- Design for extensibility (new connectors, filtering, auth).
- Consider performance and accessibility for dashboard UX.

## 3) Current Project Baseline

- UI: Blazor Server in `backend/PriorityHub.Ui/`
- Backend: ASP.NET Core in `backend/PriorityHub.Api/`
- Runtime: same host process; no HTTP boundary between UI and backend
- Connector model: multiple connector instances can run in parallel
- Local provider config: managed via Settings UI and stored locally
- Agent environment: .NET 10 via `.github/copilot-setup-steps.yml`

## 4) Common Commands

- Build solution: `dotnet build PriorityHub.sln`
- Test solution: `dotnet test PriorityHub.sln`
- Run app (watch): `dotnet watch --project backend/PriorityHub.Ui/PriorityHub.Ui.csproj run`
