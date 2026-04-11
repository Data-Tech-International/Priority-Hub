# Changelog

All notable changes to Priority Hub are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Priority Hub adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0](https://github.com/Data-Tech-International/Priority-Hub/compare/v0.3.0...v0.4.0) (2026-04-11)


### Features

* **#94:** Add telemetry unit tests, changelog, and version bump ([#94](https://github.com/Data-Tech-International/Priority-Hub/issues/94)) ([5ee9703](https://github.com/Data-Tech-International/Priority-Hub/commit/5ee9703c2c7dbcf295c16220a09eebad27c0578c))

## [Unreleased]

### Added

- Azure Application Insights telemetry integration with configuration-driven optional enablement
- Authentication sign-in/sign-out event tracking with per-provider metrics (AuthSignInCount)
- Connector fetch telemetry: status, duration, item count with per-provider metrics (ConnectorFetchCount, ConnectorFetchDuration, ConnectorItemsCount)
- Blazor page-view tracking for navigation analytics
- Configuration save and linked-account operation event tracking
- Background service for periodic active-user and registered-user count metrics (ActiveUsers, RegisteredUsers)
- ITelemetryInitializer for hashed user context enrichment across all telemetry
- No-op ITelemetryService fallback when Application Insights connection string is absent
- Comprehensive unit tests for all telemetry services, DI registration, and PII hashing

## [0.3.0](https://github.com/Data-Tech-International/Priority-Hub/compare/v0.2.0...v0.3.0) (2026-04-11)


### Features

* **#17:** dashboard UX improvements ([#18](https://github.com/Data-Tech-International/Priority-Hub/issues/18)) ([58b15dd](https://github.com/Data-Tech-International/Priority-Hub/commit/58b15dd3c94245b3bd00bb03ba00c9770dddedba))
* **#19:** add agent configuration ([#20](https://github.com/Data-Tech-International/Priority-Hub/issues/20)) ([f6d9000](https://github.com/Data-Tech-International/Priority-Hub/commit/f6d9000c0952d993fa849c254b467e48f73faa61))
* **#23:** Secure browser passphrase cache using WebCrypto AES-GCM wrapping ([#24](https://github.com/Data-Tech-International/Priority-Hub/issues/24)) ([ba867b1](https://github.com/Data-Tech-International/Priority-Hub/commit/ba867b123a24c918967f033b4427db5d2aef15a7))
* **#70:** UI onboarding redirect, import auto-save, and collapsible dashboard panels ([#71](https://github.com/Data-Tech-International/Priority-Hub/issues/71)) ([5db636c](https://github.com/Data-Tech-International/Priority-Hub/commit/5db636c72691552380f07132679f4df342fbd183))
* **#87:** default ADO WIQL Assigned-to-Me + fix release-please config ([#89](https://github.com/Data-Tech-International/Priority-Hub/issues/89)) ([4e3b6ac](https://github.com/Data-Tech-International/Priority-Hub/commit/4e3b6ac41f08d3f851c1a6cf272e38a9ee76f450))
* **#87:** default ADO WIQL to include Assigned to Me filtering ([#88](https://github.com/Data-Tech-International/Priority-Hub/issues/88)) ([3b7a3f4](https://github.com/Data-Tech-International/Priority-Hub/commit/3b7a3f49bf8778dd2d9edc12132fef97a4d1f932))
* Add app footer with version, feedback link, copyright, and license ([#49](https://github.com/Data-Tech-International/Priority-Hub/issues/49)) ([d48e326](https://github.com/Data-Tech-International/Priority-Hub/commit/d48e326e3aef2a3eb99e04bab61fe6b1e48ac42c))
* add automatic semantic versioning with release-please ([#40](https://github.com/Data-Tech-International/Priority-Hub/issues/40)) ([6ace586](https://github.com/Data-Tech-International/Priority-Hub/commit/6ace5869b5c1325142909ce31ce98cd5f5cc7ae8))
* Add TargetDate & IsBlocked fields with dashboard filters and Connectors multi-select ([#45](https://github.com/Data-Tech-International/Priority-Hub/issues/45)) ([7f2e8a7](https://github.com/Data-Tech-International/Priority-Hub/commit/7f2e8a77f5bc8895f094eb0ef5edf0ea8cbdbee3))
* Adopt PostgreSQL JSONB for config storage with seamless local development ([#32](https://github.com/Data-Tech-International/Priority-Hub/issues/32)) ([a528eae](https://github.com/Data-Tech-International/Priority-Hub/commit/a528eae14d222239a91ed9f89ce06bc8297fce9b))
* Dockerisation with PostgreSQL-backed configuration ([#34](https://github.com/Data-Tech-International/Priority-Hub/issues/34)) ([84e7a19](https://github.com/Data-Tech-International/Priority-Hub/commit/84e7a198aa517254e78f4a688c48c67334c9ea81))
* import configuration with upsert merge logic and preview confirmation ([#59](https://github.com/Data-Tech-International/Priority-Hub/issues/59)) ([cd08952](https://github.com/Data-Tech-International/Priority-Hub/commit/cd08952826183facabadb513d9c49aefe7d653a3))
* make authentication providers configurable with Enabled flag — Microsoft and GitHub on by default ([#55](https://github.com/Data-Tech-International/Priority-Hub/issues/55)) ([4523d92](https://github.com/Data-Tech-International/Priority-Hub/commit/4523d925f0a992929b20a2944cfaf85fdc52fe5c))
* Migrate frontend from Vite + React to Blazor Server ([#12](https://github.com/Data-Tech-International/Priority-Hub/issues/12)) ([820666e](https://github.com/Data-Tech-International/Priority-Hub/commit/820666e9bb98f87988ab67d48b9f2cf5f8864bce))
* open-source community infrastructure (docs, issue templates, metadata) ([#30](https://github.com/Data-Tech-International/Priority-Hub/issues/30)) ([b0171b9](https://github.com/Data-Tech-International/Priority-Hub/commit/b0171b9b59e94343c7212f09d591aad5f75d3165))
* UI improvements — dashboard, connector filter, login providers and auth icons ([#53](https://github.com/Data-Tech-International/Priority-Hub/issues/53)) ([6754a18](https://github.com/Data-Tech-International/Priority-Hub/commit/6754a18e95b2710b0f0eabc8448910f31aa960c9))


### Bug Fixes

* **#37:** differentiate Azure DevOps 404 from auth errors in error messages ([#38](https://github.com/Data-Tech-International/Priority-Hub/issues/38)) ([1e1b3d0](https://github.com/Data-Tech-International/Priority-Hub/commit/1e1b3d0a3386b78fc220ed19aa4d0315a1eb0d49))
* **#68:** restore _framework/blazor.web.js in Docker builds ([4e82f2b](https://github.com/Data-Tech-International/Priority-Hub/commit/4e82f2b386af397796390286bed2648a4b9a651f)), closes [#68](https://github.com/Data-Tech-International/Priority-Hub/issues/68)
* **#72:** span toggle row across hero-panel grid columns ([#73](https://github.com/Data-Tech-International/Priority-Hub/issues/73)) ([781f1a5](https://github.com/Data-Tech-International/Priority-Hub/commit/781f1a53ab7ea2b4752339dd8d0bac8ebd0c1320))
* add startup retry for database connection ([8c63a7d](https://github.com/Data-Tech-International/Priority-Hub/commit/8c63a7da2bbe41ae06372165a2ce627157cd5cb6))
* Add UseForwardedHeaders middleware and document WebSocket requirement for Azure deployments ([#69](https://github.com/Data-Tech-International/Priority-Hub/issues/69)) ([f82ce8b](https://github.com/Data-Tech-International/Priority-Hub/commit/f82ce8b6a4a43dbe066e8d831fd92e5eaef11a22))
* Azure DevOps connector — use DevOps-scoped token via refresh exchange ([#16](https://github.com/Data-Tech-International/Priority-Hub/issues/16)) ([5fc60dc](https://github.com/Data-Tech-International/Priority-Hub/commit/5fc60dcf1f583fcd555b08e86c46df433cd0f18c))
* **azure-devops:** stop using Graph API token as Azure DevOps fallback ([#14](https://github.com/Data-Tech-International/Priority-Hub/issues/14)) ([653ae53](https://github.com/Data-Tech-International/Priority-Hub/commit/653ae533b2c6e179020a9aa21f525831b680993c))
* **ci:** remove Node.js/npm requirements from CI workflows for Blazor migration Phase 8 ([#13](https://github.com/Data-Tech-International/Priority-Hub/issues/13)) ([e4b5c20](https://github.com/Data-Tech-International/Priority-Hub/commit/e4b5c2057db258acd0de3ef7bbe1456910112cd3))
* correct VS Code task problem matcher configuration ([#9](https://github.com/Data-Tech-International/Priority-Hub/issues/9)) ([7c13f5d](https://github.com/Data-Tech-International/Priority-Hub/commit/7c13f5d14345f03b4d2d7cd13874036afdd69f5f))
* vertically centre checkboxes in Connector and Tag filter dropdowns ([#57](https://github.com/Data-Tech-International/Priority-Hub/issues/57)) ([80734c6](https://github.com/Data-Tech-International/Priority-Hub/commit/80734c688c38fde03d9aa66c30ef62d0e9fccc16))

## [Unreleased]

### Changed
- **Default ADO WIQL includes "Assigned to Me"**: new Azure DevOps connectors now default to `[System.AssignedTo] = @me` in the WIQL query, matching the Jira connector's `assignee = currentUser()` behavior. Existing connectors are unaffected.

### Added
- **First-run onboarding redirect**: when an authenticated user has no connectors configured, the dashboard automatically redirects to `/settings?onboarding=true` with a welcome toast guiding them to configure their first connector.
- **Import auto-save and tab switch**: confirming a configuration import now automatically persists the imported settings and navigates to the Connectors tab with a success banner. On save failure, the user stays on the Import/Export tab with an error message.
- **Collapsible dashboard panels**: the hero panel (title, description, metrics) and the title/filter panel on the dashboard are now collapsible via toggle buttons. Collapse state is persisted in `localStorage`, defaults to expanded, and collapsing filters does not clear active filter state. Toggle buttons include `aria-expanded` for accessibility.
- **`ProviderConfiguration.HasAnyConnections()`**: helper method that returns `true` if any connector list contains at least one connection.

### Fixed
- **Blazor interactivity broken in Docker containers**: `_framework/blazor.web.js` was missing from the publish output when Dockerfile used `dotnet publish --no-restore` (a .NET 10 regression, see [dotnet/aspnetcore#63962](https://github.com/dotnet/aspnetcore/issues/63962)). Removed `--no-restore` flag so the SDK correctly emits `_framework/` static web assets. This caused all `@onclick` handlers, tab switching, and connector panel toggles to be non-functional on staging/production while working on localhost.
- **Forwarded headers middleware**: added explicit `app.UseForwardedHeaders()` before authentication middleware for reliable operation behind reverse proxies (Azure App Service). Previously relied only on the `ASPNETCORE_FORWARDEDHEADERS_ENABLED` environment variable.
- **Startup database connection resilience**: `ApplyDatabaseMigrationsAsync` now retries up to 5 times (with 5-second delays) on transient `NpgsqlException` failures, preventing container crashes when PostgreSQL is momentarily unreachable during cold start on Azure App Service.

### Added
- **Application Insights telemetry foundation**: introduced optional telemetry infrastructure with `ITelemetryService`, `IActiveUserTracker`, Application Insights-backed/no-op implementations, telemetry initializer user-context enrichment, and periodic active/registered user metric emission groundwork for issue #79.
- **Specification**: multi-source email aggregation with credential encryption and linked Microsoft accounts — full spec added at `plans/specifications/spec-62-spec-multi-source-email-aggregation-with-credential-encryption.md` covering Phase 1 (backend credential encryption via .NET Data Protection API), Phase 2 (IMAP flagged-mail connector using MailKit), and Phase 3 (linked Microsoft accounts with per-connection account selection).
- **Backend credential encryption (Phase 1)**: all sensitive connector credential fields (`PersonalAccessToken`, `ApiToken`, `ApiKey`, `Token`, `Password`, `RefreshToken`) are now encrypted at rest using .NET Data Protection API with a file-system key ring (`config/keys/`). Both `LocalConfigStore` (file) and `PostgresConfigStore` (PostgreSQL) apply encryption transparently via a new `EncryptingConfigStore` decorator. Existing plaintext configs migrate automatically on the first load+save cycle.
- **IMAP Flagged Mail connector (Phase 2)**: a new connector (`imap-flagged-mail`) fetches flagged and keyword-tagged messages from any IMAP server over implicit TLS (port 993). Supports Gmail, Outlook.com, Yahoo, and other providers. Configured with IMAP server, email, app password, folder path, custom keywords, and max results. Passwords are encrypted at rest (Phase 1). Uses MailKit.
- **Linked Microsoft Accounts (Phase 3)**: users can link additional Microsoft accounts from **Settings → Account** without changing their login session. Each linked account's refresh token is stored encrypted and exchanged for a Graph API access token at dashboard load time. Outlook Flagged Mail and Microsoft Tasks connections gain a **Microsoft Account** dropdown to select primary or any linked account. Endpoints: `GET /api/auth/link/microsoft`, `GET /api/auth/link/microsoft/callback`, `DELETE /api/auth/link/microsoft/{accountId}`.
- **`[SensitiveField]` attribute**: marks string properties for at-rest encryption. Currently applied to `AzureDevOpsConnection.PersonalAccessToken`, `JiraConnection.ApiToken`, `GitHubConnection.PersonalAccessToken`, `TrelloConnection.ApiKey`, `TrelloConnection.Token`, `ImapFlaggedMailConnection.Password`, `LinkedMicrosoftAccount.RefreshToken`.
- **`config/keys/` excluded from version control**: added to `.gitignore` to prevent accidental commit of Data Protection encryption keys.

### Changed
- **`DashboardAggregator`**: `BuildPendingFetches` now resolves per-connection OAuth tokens for connections with a `LinkedAccountId`, falling back to the provider-level token for connections without one.
- **`OauthTokenService`**: added `GetLinkedAccountTokensAsync` to exchange stored linked account refresh tokens for Microsoft Graph access tokens, keyed by connection ID.

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
