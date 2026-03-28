# Processes

This section describes the development and contribution processes for Priority Hub.

## Local Development Setup

### Prerequisites

- .NET 10 SDK
- Node.js 20+ and npm
- Docker (for the local PostgreSQL container)

### Bootstrap (fresh clone)

```bash
# 1. Copy the Development configuration example (one-time step)
cp backend/PriorityHub.Ui/appsettings.Development.example.json \
   backend/PriorityHub.Ui/appsettings.Development.json

# 2. Start the local database
docker compose up -d

# 3. Start the application with hot reload
npm run dev
```

The application auto-applies pending schema migrations on startup in Development.
No manual `CREATE TABLE` steps are required.

### Stopping and resetting the database

```bash
# Stop the container but preserve data
docker compose down

# Full reset (removes the data volume)
docker compose down -v
```

### Quick verification checklist

After a fresh bootstrap:

- [ ] `appsettings.Development.json` copied from the example file.
- [ ] `docker compose ps` shows `postgres` in a healthy state.
- [ ] `npm run dev` starts without errors.
- [ ] Navigating to `http://localhost:5000` shows the login page.
- [ ] Signing in, adding a connector, and restarting the app persists the connector.

### Using file-based storage (no Docker)

Override `ConfigStore:Provider` in user secrets or a local override file:

```bash
dotnet user-secrets set "ConfigStore:Provider" "File" --project backend/PriorityHub.Ui
```

## Specification-First Workflow

Major changes follow a specification-first delivery process.

1. **Create a specification issue** – label it `specification`.
2. **Generate a plan file** – save to `plans/specifications/` using the spec file naming convention.
3. **Review and approve** – label the issue `plan-approved` after team review.
4. **Implement on a feature branch** – open a draft pull request early.
5. **Validate** – run `dotnet build PriorityHub.sln` and `dotnet test PriorityHub.sln`.
6. **Update documentation** – update `README.md`, `docs/`, and the plan file in the same PR.
7. **Request review** – mark the pull request ready for review.

## Versioning

Priority Hub follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html) (`MAJOR.MINOR.PATCH`).

| Segment | When to increment |
|---------|-------------------|
| MAJOR | Breaking changes to public interfaces or behavior |
| MINOR | New backward-compatible features |
| PATCH | Backward-compatible bug fixes |

**Version synchronization rule:** `package.json` and `backend/Directory.Build.props` must always carry the same version string. Update both in the same commit when cutting a release.

## Changelog Maintenance

The project uses [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format.

- Every behavior change **must** include an entry in `[Unreleased]`.
- Use the standard subsections: `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`.
- On release: rename `[Unreleased]` to the version tag, add a new empty `[Unreleased]` section, and update the comparison links at the bottom of `CHANGELOG.md`.

## Branch and Commit Conventions

- Never push directly to `main`.
- Use conventional commits: `feat(#N): description`, `fix(#N): description`, `docs(#N): description`.
- Keep commits focused; one logical change per commit.

## CI Checks

Pull requests and main-branch pushes run:

1. Coding standards checks.
2. Security scanning (dependencies and secret detection).
3. Static analysis.
4. Test and coverage validation.

For agent and workflow details see [`.github/agents/`](../../.github/agents/) and [`.github/MCP-INTEGRATION.md`](../../.github/MCP-INTEGRATION.md).

## Related

- [Troubleshooting](../troubleshooting/README.md)
- [Back to docs index](../README.md)

## Contributing

External contributors are welcome. See [CONTRIBUTING.md](../../CONTRIBUTING.md) at the repository
root for the full contributor guide, including development setup, coding standards, commit
conventions, and the pull request process.
