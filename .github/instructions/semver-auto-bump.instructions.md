---
description: "Use when committing changes, preparing PRs, or finalizing work that modifies behavior. Enforces semantic versioning by determining the correct MAJOR/MINOR/PATCH bump and updating Directory.Build.props automatically."
applyTo: "backend/Directory.Build.props"
---

# Automatic Semantic Version Bump

Every commit that changes runtime behavior **must** bump the version in
`backend/Directory.Build.props` (the single source of truth).

## 1 — Determine the bump type

### Primary: conventional commit prefix

| Prefix / trailer             | Bump  | Example                                    |
|------------------------------|-------|--------------------------------------------|
| `BREAKING CHANGE` trailer    | MAJOR | Any commit with `BREAKING CHANGE:` footer  |
| `!` after type (e.g. `feat!`)| MAJOR | `feat!: remove legacy auth`                |
| `feat`                       | MINOR | `feat: add IMAP connector`                 |
| `fix`, `perf`                | PATCH | `fix: null-ref in dashboard aggregator`    |
| `refactor`, `style`, `test`, `docs`, `chore`, `ci`, `build` | NONE | No version bump needed |

### Fallback: CHANGELOG.md `[Unreleased]` sections

If the commit message does not use conventional prefixes, inspect the
`[Unreleased]` section of `CHANGELOG.md`:

| Section present             | Bump  |
|-----------------------------|-------|
| `Removed` or `Security`    | MAJOR (if public API surface changed) |
| `Added`                     | MINOR |
| `Changed`, `Deprecated`    | MINOR |
| `Fixed`                     | PATCH |

When multiple sections exist, use the **highest** bump (MAJOR > MINOR > PATCH).

## 2 — Apply the bump

Update **all four** version properties in `backend/Directory.Build.props`:

```xml
<Version>X.Y.Z</Version>
<AssemblyVersion>X.Y.Z.0</AssemblyVersion>
<FileVersion>X.Y.Z.0</FileVersion>
<InformationalVersion>X.Y.Z</InformationalVersion>
```

Rules:
- Never skip a version number.
- Reset lower segments when a higher segment increments:
  `0.2.1` → MINOR → `0.3.0`, not `0.3.1`.
- Pre-1.0: treat MINOR as the "breaking" segment if needed.

## 3 — What does NOT require a bump

- Documentation-only changes (`docs/`, `README.md`, `plans/`, `*.md` outside source).
- Test-only changes (`*.Tests/` projects).
- CI/workflow changes (`.github/workflows/`).
- Code style / formatting with no behavior change.
- Adding or updating agent/instruction/prompt files.

## 4 — Verification

After bumping, confirm the build succeeds:

```
dotnet build PriorityHub.sln
```
