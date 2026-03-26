# Specification: spec: Make PriorityHub an open-source project with contribution documentation

## Metadata
- Source issue: #29
- Source URL: https://github.com/Data-Tech-International/Priority-Hub/issues/29
- Author: @ipavlovi
- Created: 2026-03-26T11:57:31Z

## Specification

## Problem Statement

PriorityHub is currently a private repository without community contribution infrastructure. Before making the repository public, several critical gaps must be addressed:

1. **Security**: A `.env` file containing a GitHub PAT (`ghp_mGchmjrmY...`) is committed to git history and must be scrubbed before the repo goes public.
2. **Community docs**: No `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, or `SECURITY.md` exist to guide contributors.
3. **Issue templates**: Only a specification template exists — no bug report or feature request templates for community use.
4. **Open-source metadata**: `package.json` is marked `private: true` and lacks `repository`, `bugs`, `homepage`, `license`, `description`, `keywords`, and `author` fields.
5. **README gaps**: No license badge, CI badge, Contributing section, License section, or Security reference.

## Goals and Outcomes

- PriorityHub is safe to make public (no committed secrets in history).
- External contributors have a clear, documented path to participate.
- Community norms are established via Code of Conduct.
- Vulnerability reporting has a defined responsible disclosure process.
- Issue templates cover the three main contribution paths: bug reports, feature requests, and specifications.
- Open-source metadata is complete and accurate.

## Acceptance Criteria

### Phase 1: Security Remediation (blocking)
- [ ] Exposed GitHub PAT is revoked at https://github.com/settings/tokens
- [ ] `.env` file is purged from all git history using `git filter-repo` or BFG Repo-Cleaner
- [ ] `.env` remains in `.gitignore` after history rewrite
- [ ] `git log --all --full-history -- .env` returns empty after scrub

### Phase 2: Community Foundation Files
- [ ] `CODE_OF_CONDUCT.md` exists at repo root with Contributor Covenant v2.1 text and maintainer contact
- [ ] `SECURITY.md` exists at repo root with responsible disclosure policy (scope, reporting method, response timeline)
- [ ] `CONTRIBUTING.md` exists at repo root covering:
  - Welcome statement + link to Code of Conduct
  - Ways to contribute (bugs, features, code, docs, testing)
  - Development environment setup (prerequisites, clone, build, test)
  - Specification-first workflow for major changes
  - Bug fix / small change workflow (fork → branch → PR)
  - Coding standards (4-space indent, PascalCase/\_camelCase, XML docs)
  - Commit conventions (conventional commits: `feat(#N):`, `fix(#N):`, `docs(#N):`)
  - PR checklist summary (reference `.github/pull_request_template.md`)
  - Testing requirements (`dotnet build` + `dotnet test`, 60% coverage threshold)
  - Documentation update expectations
  - Review process
  - License notice (MIT, no CLA/DCO required)

### Phase 3: Issue Templates
- [ ] `.github/ISSUE_TEMPLATE/bug_report.yml` created with fields: description, reproduction steps, expected vs actual, environment, screenshots/logs. Auto-label: `bug`
- [ ] `.github/ISSUE_TEMPLATE/feature_request.yml` created with fields: problem/use case, proposed solution, alternatives. Auto-label: `enhancement`. Note about spec-first for major features
- [ ] `.github/ISSUE_TEMPLATE/config.yml` created as template chooser listing all three templates

### Phase 4: Metadata & README Updates
- [ ] `package.json` updated: `private: false`, plus `description`, `repository`, `bugs`, `homepage`, `license`, `keywords`, `author` fields
- [ ] `README.md` updated with: license badge, CI status badge, "Contributing" section linking to CONTRIBUTING.md, "License" section, "Security" reference to SECURITY.md
- [ ] `docs/processes/README.md` updated with Contributing section linking to root CONTRIBUTING.md
- [ ] `CHANGELOG.md` updated with entries under `[Unreleased]` → `[Added]` for all new files and metadata

### Verification
- [ ] `dotnet build PriorityHub.sln` passes
- [ ] `dotnet test PriorityHub.sln` passes
- [ ] All cross-references between CONTRIBUTING.md, README.md, and docs resolve correctly
- [ ] `npm pkg get` shows repository, bugs, homepage, license fields

## Constraints and Non-Goals

### In Scope
- Community documentation (CONTRIBUTING.md, CODE_OF_CONDUCT.md, SECURITY.md)
- Issue templates (bug report, feature request, template chooser config)
- Open-source metadata in package.json and README.md
- Git history scrub of committed `.env` secret
- CHANGELOG.md updates

### Out of Scope
- GitHub UI settings changes (private → public toggle, branch protection rules, Discussions enablement) — manual steps by maintainer
- CODEOWNERS file (add later when contributor base grows)
- CLA/DCO enforcement — not required per project decision
- Changes to AGENTS.md or .github/copilot-instructions.md (internal agent docs, not contributor-facing)
- Changes to CI workflows

### Decisions Made
- **Code of Conduct**: Contributor Covenant v2.1
- **No CLA/DCO required**: contributions fall under MIT license
- **Issues only** for community communication (no GitHub Discussions reference)
- **Git history rewrite required**: all collaborators must re-clone after scrub

## Testing Expectations

- Build verification: `dotnet build PriorityHub.sln`
- Test suite: `dotnet test PriorityHub.sln`
- Link validation: all relative links in new/modified markdown files resolve correctly
- Security verification: `git log --all --full-history -- .env` returns empty

## Documentation Impact

### New Files
- `CONTRIBUTING.md` — contributor guide (repo root)
- `CODE_OF_CONDUCT.md` — Contributor Covenant v2.1 (repo root)
- `SECURITY.md` — responsible disclosure policy (repo root)
- `.github/ISSUE_TEMPLATE/bug_report.yml` — bug report template
- `.github/ISSUE_TEMPLATE/feature_request.yml` — feature request template
- `.github/ISSUE_TEMPLATE/config.yml` — template chooser

### Modified Files
- `README.md` — badges, Contributing/License/Security sections
- `package.json` — open-source metadata
- `docs/processes/README.md` — Contributing section
- `CHANGELOG.md` — [Unreleased] entries

### Note for Implementer
The Code of Conduct and SECURITY.md require a real contact email for reports. Use the project maintainer's email or configure GitHub Security Advisories as the reporting channel for SECURITY.md.


## Clarifications
- [ ] Confirm assumptions before planning if anything is unclear.
