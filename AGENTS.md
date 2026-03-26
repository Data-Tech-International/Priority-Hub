# Priority Hub - Coding Agent Guide

Purpose: implement approved work safely and consistently in Priority Hub (Blazor Server UI + ASP.NET Core 10 backend-for-frontend).

## 1) Must Follow

### Versioning and Changelog (Mandatory)
- Follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html): `MAJOR.MINOR.PATCH`.
  - Increment MAJOR for breaking changes, MINOR for new features, PATCH for bug fixes.
- Keep `package.json` and `backend/Directory.Build.props` at the same version string at all times. Update both in the same commit when cutting a release.
- Every behavior change **must** add an entry under `[Unreleased]` in `CHANGELOG.md` using the [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format.
  - Use the standard subsections: `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`.
- On release: rename `[Unreleased]` to the new version tag, open a new empty `[Unreleased]` section, and update the comparison links at the bottom of `CHANGELOG.md`.

### Process
1. Read the linked specification issue and plan in `plans/` before coding.
2. Implement only the assigned scope.
3. Update or add tests for every behavior change.
4. Update documentation when behavior, setup, or usage changes.
5. Run full verification before completion:
   - `dotnet build PriorityHub.sln`
   - `dotnet test PriorityHub.sln`

### Documentation Environment Setup
1. Confirm tools are available:
   - .NET 10 SDK (`dotnet --version`)
   - Node 20+ and npm (for repo script wrappers)
2. Validate documentation commands from repo root:
   - `dotnet build PriorityHub.sln`
   - `dotnet test PriorityHub.sln`
3. Verify documentation examples are runnable and match actual behavior:
   - Startup command(s)
   - Build and test command(s)
   - Referenced file paths and folder names
4. If behavior changed, update `README.md` and the active plan in `plans/` in the same implementation.

### Code Placement
- Backend production code: `backend/PriorityHub.Api/`
- Backend tests: `backend/PriorityHub.Api.Tests/`
- UI production code: `backend/PriorityHub.Ui/`
- UI tests: `backend/PriorityHub.Ui.Tests/`
- Plans and specs: `plans/`

### Security and Safety
- Never hardcode secrets, API keys, or tokens.
- Never commit `config/providers.local.json`.
- Validate external input at API boundaries.
- Route external HTTP calls through connector services in `backend/PriorityHub.Api/Services/Connectors/`.

### Guardrails
- Do not modify files outside implementation scope.
- Do not remove or weaken existing tests.
- Do not change CI/workflow files unless the plan explicitly requires it.
- Do not push directly to `main`.

### Documentation Rules
- Treat `README.md` as the canonical user setup and usage document.
- Keep instructions task-based: prerequisites, steps, expected result, and troubleshooting.
- Ensure all commands are copy-paste ready and tested in this repository.
- Keep terminology consistent across README, plans, and UI labels.
- Do not document unshipped or speculative behavior as current functionality.

## 2) Recommended

### Commit Hygiene
- Use focused, logical commits.
- Prefer conventional commits referencing the issue, for example: `feat(#42): add input validation`.

### Backend Conventions
- 4-space indentation.
- PascalCase for public members; `_camelCase` for private fields.
- Add XML documentation to public classes and methods.
- Use dependency injection for services.

### Frontend Conventions
- 4-space indentation.
- PascalCase for component parameters and public members.
- Prefer dependency injection over direct HTTP usage in components.
- Use JS interop only when Blazor has no native option.

### Documentation Quality
- Keep `README.md` aligned with actual setup and usage.
- Mark completed steps in the related plan file.
- Prefer short sections with clear headings and step-by-step procedures.
- Include "why" only when it helps users avoid common mistakes.
- Add troubleshooting notes for known failure modes and recovery steps.
- Use examples with realistic values and clearly mark placeholders.

## 3) Implementation Checklist

1. Confirm spec + plan are approved and understood.
2. Implement production changes in the correct project.
3. Add/update tests in corresponding test project.
4. Update docs and plan status.
5. Run build and test commands.
6. Prepare clean commit(s) and PR-ready summary.
