# Changelog

All notable changes to Priority Hub are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Priority Hub adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `backend/Directory.Build.props` for centralized .NET version alignment with `package.json`.
- `CHANGELOG.md` in Keep a Changelog format.
- `docs/` markdown skeleton covering features, configuration, processes, and troubleshooting.
- Semantic Versioning and changelog governance rules in `AGENTS.md` and `.github/copilot-instructions.md`.

## [0.2.0] - 2025-01-01

### Added
- Multi-connector model supporting multiple Azure DevOps, Jira, and Trello instances in parallel.
- Manual drag-and-drop ordering persisted across all sources.
- New-item highlighting until items are placed in the preferred order.
- Blazor Server UI with ASP.NET Core 10 backend-for-frontend (BFF).
- Normalized work item model across all providers.
- Ranked priority queue using impact, urgency, confidence, blockers, age, and effort.
- Dashboard with connector health indicators, filtering, and a single personal priority view.
- Settings UI for managing provider credentials stored in `config/providers.local.json`.
- Specification-first delivery workflow with plan files in `plans/`.

[Unreleased]: https://github.com/Data-Tech-International/Priority-Hub/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/Data-Tech-International/Priority-Hub/releases/tag/v0.2.0
