# Changelog

All notable changes to Priority Hub are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Priority Hub adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- **Agent definitions normalized**: removed VS Code-specific fields (`tools`, `model`, `target`) from all `.github/agents/*.agent.md` files; normalized quote styles to single quotes and reordered frontmatter (`name` before `description`) to align with GitHub Copilot agent format standards.

### Added
- **Quick Start guide**: new `docs/quick-start.md` walks end users through sign-in, connector setup, and dashboard usage, covering all six providers with example field values.
- **Export download**: the "Download configuration" button now triggers a real browser file download (`priority-hub-config.json`) via JS interop instead of only updating the status message.
- **Import connector configuration**: a new "Import connector configuration" card on the Import / Export tab lets users upload a previously exported JSON file (max 1 MB). Connections are merged with upsert logic: connections matched by `id` are updated; new `id`s are inserted; connections absent from the file are left unchanged. Preferences (manual ordering) are never overwritten.
- **Import preview**: after selecting a file, a preview panel summarises how many connections will be added, updated, or unchanged, lists each affected connection by name and provider, and warns if masked secrets (`********`) are detected.
- **Import / Export tab renamed**: the "Export" settings tab is now labelled "Import / Export".
- **Configurable authentication providers**: each auth provider section in `appsettings.json` now supports an `Enabled` boolean flag. Microsoft and GitHub default to `true`; Google, Facebook, Jira, Trello, and Yandex default to `false`.
- **Dynamic login page**: the login page now reads the `Enabled` flag from configuration and renders only enabled provider buttons, hiding disabled providers entirely.
- **503 guard on disabled provider endpoints**: hitting `/api/auth/login/{provider}` for a disabled provider returns `503 Service Unavailable` regardless of whether `ClientId`/`ClientSecret` are set.

- **Login authentication providers**: Jira, Yandex, Trello, Google, and Facebook added as OAuth sign-in options on the login page
- **Provider icons on login page**: each authentication provider button now displays an inline SVG icon next to the provider name
- **Settings tab icons**: each tab (Connectors, Account, Export, Security) now shows an emoji icon
- **Settings tab keyboard navigation**: arrow keys (←/→), Home, and End move focus between tabs; matches WAI-ARIA tab pattern
- **Collapsible connector sections**: provider sections are collapsed by default; clicking the header expands/collapses and shows a connection-count summary in the collapsed state
- **Remove confirmation**: clicking "Remove" on a connection now shows a browser confirmation dialog before deleting
- **Inline field validation**: text and password fields show error messages immediately on change, not only on save
- **Unsaved changes indicator**: an "Unsaved changes" badge appears in the save footer whenever edits have not yet been saved
- **Loading spinner**: initial config load shows an animated spinner instead of plain text
- **Save button spinner**: the Save button shows an animated spinner while saving
- **Dismissible status messages**: the status banner now includes a dismiss (✕) button

### Changed
- **Dashboard connector filter**: now filters by connector instance (connection ID and board name) instead of provider type
- **Dashboard panel header**: removed the "Drag cards to keep one manual order across all connected sources." instructional text
- **Dashboard work item cards**: untagged items no longer show a "No tags" pill; the tag row is rendered empty instead
- Status banner uses distinct error (red) vs. info (blue) styling based on outcome
- "Add connection" button moved inside each expanded connector section (below the connection list)
- Settings tab buttons use ARIA roles (`role="tablist"`, `role="tab"`, `aria-selected`, `aria-controls`)
- Tab panel containers use `role="tabpanel"` with matching `aria-labelledby`
- Validation error `<small>` elements include `role="alert"` for screen-reader announcement

### Fixed
- **Login page heading border**: removed visible border on the "Sign in to access your unified priority dashboard." heading
- **Checkbox alignment in filter dropdowns**: checkboxes in Connector and Tag filter dropdowns are now vertically centred with the label text (`margin: 0`, fixed size, `accent-color` for brand styling)

- Footer with app version, feedback link, copyright, and license information
- **Connector instance emoji**: each connector connection now has a user-selectable emoji field (`emoji` in JSON config)
- **Default emojis per connector type**: 🔷 Azure DevOps, 🐙 GitHub, 📋 Jira, 📌 Trello, ✅ Microsoft Tasks, 📧 Outlook Flagged Mail
- **`EmojiPicker` Blazor component**: pure C#/Blazor emoji picker with categorised groups, keyword search, and selected-state highlight
- **`EmojiData` service**: curated emoji dataset (7 categories, keyword search, single-emoji validation via `StringInfo`)
- **`DefaultEmoji` on `IConnector`**: each connector declares its type-level default emoji
- **`Emoji` on `BoardConnection` model**: carried through from connector config to dashboard payload
- **`Emoji` on `RankedWorkItem`**: propagated from `BoardConnection` for use in UI
- **`WorkItemRanker.GetProviderEmoji`**: static helper returning default emoji for a provider key
- Emoji displayed at 24 px before work item title on dashboard cards
- Emoji displayed before board name on connector status cards in dashboard
- Emoji displayed before connector name in Connectors filter dropdown
- Emoji picker displayed in connection editor cards on the Settings page
- Emoji displayed in provider section headers on the Settings page
- Legacy config without an `emoji` field falls back to the connector type's default (no errors)
- Emoji field validation rejects strings with more than one grapheme cluster
- `TargetDate` (`DateTimeOffset?`) and `IsBlocked` (`bool`) fields to `WorkItem` model
- Azure DevOps: `TargetDate` from `Microsoft.VSTS.Scheduling.TargetDate`; `IsBlocked` from blocked state
- Jira: `TargetDate` from `fields.duedate`; `IsBlocked` from blocked status
- Trello: `TargetDate` from `card.due`; `IsBlocked` from "blocked" label
- Microsoft Tasks: `TargetDate` from `dueDateTime`; `IsBlocked` from "waitingOnOthers" status
- GitHub Issues: `IsBlocked` from "blocked" label
- Dashboard Blocked filter: All / Blocked only / Not blocked
- Dashboard Target date filter: All / Has target date / No target date / Overdue / Due within 7 days
- "Blocked" pill and target date countdown displayed on work item cards
- `.is-blocked` CSS class and warm background tint on blocked item cards
- Multi-select Connectors filter (dynamic from live board connections)

### Changed
- Priority scoring formula now includes `IsBlocked` boost (+6 points)
- Provider single-select filter replaced with multi-select Connectors filter

### Removed
- `package.json` deleted — Node.js is not a project dependency; all scripts were pure `dotnet` wrappers.
- Node.js/npm prerequisites removed from `README.md`, `CONTRIBUTING.md`, `AGENTS.md`, and `docs/`.
- npm commands replaced with dotnet equivalents in PR template, spec-plan workflow, agent definitions, and MCP integration docs.
- `node_modules/` entries removed from `.gitignore` and `.dockerignore`.
- `npm.cmd` and `npx.cmd` auto-approve entries removed from VS Code settings.

### Changed
- `.release-pleaserc.json` switched from `"type": "node"` to `"type": "simple"`; `backend/Directory.Build.props` is now the sole version source of truth.
- `backend/Directory.Build.props` comment updated to reflect single source of truth (no `package.json` sync).
- Completed plan files (`plans/blazor-migration.md`, `plans/ci-agents.md`) annotated to mark historical npm references.

### Fixed
- PostgreSQL integration tests: replaced `UntilPortIsAvailable` wait strategy with `pg_isready` command check to avoid "database system is starting up" errors in CI.

### Added
- `IConfigStore` interface abstracting per-user configuration persistence (`PriorityHub.Api/Services/IConfigStore.cs`).
- `PostgresConfigStore` storing configuration as JSONB with a monotonically increasing `version` column for write auditing (`PriorityHub.Api/Services/PostgresConfigStore.cs`).
- `SchemaManager` that auto-applies SQL migrations embedded in the assembly; auto-runs in Development and fails fast on mismatch in other environments (`PriorityHub.Api/Data/SchemaManager.cs`).
- SQL migration `0001_initial_schema.sql` creating the `schema_migrations` and `user_config` tables.
- `ConfigStoreServiceExtensions.AddConfigStore` selects `Postgres` or `File` provider based on `ConfigStore:Provider` configuration key (`PriorityHub.Api/Extensions/ConfigStoreServiceExtensions.cs`).
- `ConfigStore:Provider` and `ConfigStore:ConnectionString` configuration keys in `appsettings.json` (default: `File`).
- `appsettings.Development.json` in both `PriorityHub.Ui` and `PriorityHub.Api` defaulting to `Postgres` with local container credentials.
- `docker-compose.yml` at the repository root providing a one-command PostgreSQL 16 container for local development.
- Integration tests for `PostgresConfigStore` and `SchemaManager` using ephemeral `Testcontainers.PostgreSql` containers (`PriorityHub.Api.Tests/PostgresConfigStoreIntegrationTests.cs`).
- Contributing guide, issue templates, CI badges, docs skeleton, semantic versioning governance.
- Multi-stage `Dockerfile` at the repository root targeting `backend/PriorityHub.Ui/PriorityHub.Ui.csproj`; uses .NET 10 SDK for build and ASP.NET Core 10 runtime for the final image; exposes port `8080`.
- `.dockerignore` excluding build outputs, secrets, editor caches, and local config from the Docker build context.
- GitHub Actions workflow `.github/workflows/docker-image.yml` — builds on every pull request to `main`, builds and publishes to GHCR on push to `main`, and supports `workflow_dispatch` on any branch; adds OCI labels and commit-SHA/branch tags; cancels superseded runs per ref.

### Fixed
- `DashboardPage` and `SettingsPage` now inject `IConfigStore` instead of the concrete `LocalConfigStore`, resolving the `InvalidOperationException` at runtime when only the interface is registered in DI.
- Azure DevOps connector now retries with configured PAT when Microsoft bearer token yields HTML sign-in/auth failures, avoiding false PAT guidance when a valid PAT is already provided.
- Microsoft OAuth refresh-token exchange now requests Azure DevOps token using `https://app.vssps.visualstudio.com/user_impersonation` with GUID-scope fallback, reducing invalid Azure DevOps bearer token responses for Microsoft sign-in users.

### Changed
- `LocalConfigStore` now implements `IConfigStore`.
- `DashboardAggregator` depends on `IConfigStore` instead of the concrete `LocalConfigStore`.
- Both `Program.cs` files register `IConfigStore` via `AddConfigStore` and run `ApplyDatabaseMigrationsAsync` before `app.Run()`.
- README updated with Docker, local database bootstrap, and project structure changes.
- README added Container Quickstart, deployment configuration guidance, and container troubleshooting.
- `docs/processes/README.md` updated with Docker image lifecycle section.
- `docs/troubleshooting/README.md` updated with container-specific troubleshooting entries.

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
