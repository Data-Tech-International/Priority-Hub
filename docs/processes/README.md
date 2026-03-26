# Processes

This section describes the development and contribution processes for Priority Hub.

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
