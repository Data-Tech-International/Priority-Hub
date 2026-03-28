# Contributing to Priority Hub

Thank you for your interest in contributing to Priority Hub! This document explains how to
participate, what to expect, and how to make your contribution successful.

By contributing, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

---

## Table of Contents

- [Ways to Contribute](#ways-to-contribute)
- [Development Environment Setup](#development-environment-setup)
- [Specification-First Workflow (major changes)](#specification-first-workflow-major-changes)
- [Bug Fix and Small Change Workflow](#bug-fix-and-small-change-workflow)
- [Coding Standards](#coding-standards)
- [Commit Conventions](#commit-conventions)
- [Pull Request Checklist](#pull-request-checklist)
- [Testing Requirements](#testing-requirements)
- [Documentation Updates](#documentation-updates)
- [Review Process](#review-process)
- [License](#license)

---

## Ways to Contribute

- **Report a bug** — open a [bug report issue](.github/ISSUE_TEMPLATE/bug_report.yml).
- **Request a feature** — open a [feature request issue](.github/ISSUE_TEMPLATE/feature_request.yml).
- **Write code** — fix bugs or implement approved feature requests.
- **Improve docs** — clarify setup guides, add troubleshooting notes, or fix typos.
- **Write tests** — improve coverage for existing functionality.

---

## Development Environment Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) and npm

Verify your environment:

```bash
dotnet --version   # should print 10.x.x
node --version     # should print v20.x.x or higher
npm --version
```

### Clone and Build

```bash
git clone https://github.com/Data-Tech-International/Priority-Hub.git
cd Priority-Hub
dotnet build PriorityHub.sln
```

### Run Tests

```bash
dotnet test PriorityHub.sln
```

### Start the App (hot reload)

```bash
npm run dev
# or directly:
dotnet watch --project backend/PriorityHub.Ui/PriorityHub.Ui.csproj run
```

---

## Specification-First Workflow (major changes)

New features, significant refactors, or anything that changes public behavior require a
specification before implementation begins.

1. **Open a specification issue** — use the [Specification template](.github/ISSUE_TEMPLATE/specification.yml) and apply the `specification` label.
2. **Wait for a plan file** — a maintainer or agent will create a plan in `plans/`.
3. **Get `plan-approved` label** — implementation starts only after the plan is approved.
4. **Implement on a feature branch** — open a draft pull request early against `main`.
5. **Validate** — run `dotnet build PriorityHub.sln` and `dotnet test PriorityHub.sln`.
6. **Update docs** — update `README.md`, `docs/`, `CHANGELOG.md`, and the plan in the same PR.
7. **Request review** — mark the PR ready and request a maintainer review.

---

## Bug Fix and Small Change Workflow

For bug fixes, documentation improvements, and small changes that do not need a specification:

1. **Fork** the repository and create a branch from `main`:

   ```bash
   git checkout -b fix/issue-123-short-description
   ```

2. **Make your changes** — keep commits focused and reference the issue number.

3. **Run verification**:

   ```bash
   dotnet build PriorityHub.sln
   dotnet test PriorityHub.sln
   ```

4. **Push** your branch and **open a pull request** against `main`.

5. Fill in the [pull request template](.github/pull_request_template.md) completely.

---

## Coding Standards

### General

- 4-space indentation (enforced by `.editorconfig`).
- No trailing whitespace; files end with a single newline.

### C# Backend (`backend/`)

- PascalCase for public types and members.
- `_camelCase` for private fields.
- Add XML documentation comments (`/// <summary>`) to all public classes and methods.
- Use dependency injection — avoid `new` for services.
- Place backend production code in `backend/PriorityHub.Api/`.
- Place backend tests in `backend/PriorityHub.Api.Tests/`.

### Blazor UI (`backend/PriorityHub.Ui/`)

- PascalCase for component parameters and public members.
- Prefer DI over direct HTTP usage in Razor components.
- Use JS interop only when Blazor has no native option.
- Place UI tests in `backend/PriorityHub.Ui.Tests/`.

### Format Check

```bash
dotnet format PriorityHub.sln
dotnet build PriorityHub.sln /p:EnableNETAnalyzers=true
```

---

## Commit Conventions

Use [Conventional Commits](https://www.conventionalcommits.org/) referencing the issue number:

| Type | When to use |
|------|-------------|
| `feat(#N):` | New feature or behavior |
| `fix(#N):` | Bug fix |
| `docs(#N):` | Documentation only |
| `test(#N):` | Adding or updating tests |
| `refactor(#N):` | Code restructure without behavior change |
| `chore(#N):` | Tooling, dependencies, config |

Example:

```
feat(#42): add Trello connector health indicator
```

---

## Pull Request Checklist

Before marking your PR ready for review, confirm:

- [ ] `dotnet build PriorityHub.sln` passes with no errors.
- [ ] `dotnet test PriorityHub.sln` passes.
- [ ] New or changed behavior has test coverage (see [Testing Requirements](#testing-requirements)).
- [ ] `CHANGELOG.md` has an entry under `[Unreleased]`.
- [ ] Documentation updated where behavior changed (README, `docs/`, plan file).
- [ ] No secrets, credentials, or `config/providers.local.json` committed.
- [ ] PR description references the related issue.

See [`.github/pull_request_template.md`](.github/pull_request_template.md) for the full template.

---

## Testing Requirements

- Run the full test suite: `dotnet test PriorityHub.sln`.
- Maintain the existing coverage threshold (≥ 60% line coverage on backend services).
- Add unit tests for any new service logic.
- Add component tests (bUnit) for new Blazor components with non-trivial behavior.
- Do not remove or weaken existing tests.

---

## Documentation Updates

Every behavior change requires a documentation update in the same PR:

- Add an entry in `CHANGELOG.md` under `[Unreleased]`.
- Update `README.md` if setup, usage, or commands change.
- Update the relevant file in `docs/` if feature or configuration docs change.
- Update the plan file in `plans/` if it tracks the feature.

---

## Review Process

- Maintainers aim to review pull requests within a few business days.
- At least one maintainer approval is required to merge.
- CI checks (build, test, security, static analysis) must pass before merge.
- Feedback is provided as review comments; please respond to all open threads.
- Maintainers may request changes before approving.

---

## License

By contributing to Priority Hub, you agree that your contributions will be licensed under the
[MIT License](LICENSE). No Contributor License Agreement (CLA) or Developer Certificate of
Origin (DCO) signature is required.
