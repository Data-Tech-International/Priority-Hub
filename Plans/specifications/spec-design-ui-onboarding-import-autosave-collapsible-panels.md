---
title: "UI Overhaul: First-Run Redirect, Import Auto-Save, and Collapsible Dashboard Panels"
version: 1.0
date_created: 2025-04-07
owner: Priority Hub team
tags: [design, ui, ux, onboarding, dashboard, settings]
---

# Introduction

This specification covers three targeted UI improvements to Priority Hub:

1. **First-run onboarding redirect** — When an authenticated user has no connectors configured, automatically redirect them from the dashboard to the Settings page and display a toast message guiding them to configure their first connector.
2. **Import auto-save and tab switch** — After a configuration import is confirmed, automatically persist the imported settings and navigate the user to the Connectors tab so they can review the result.
3. **Collapsible dashboard sections** — Make the hero panel (title, description, metrics) and the title/filter panel on the main dashboard page collapsible so power users can maximize the work-item list area.

## 1. Purpose & Scope

**Purpose**: Improve the new-user onboarding experience, reduce friction after config import, and let returning users collapse informational sections they no longer need.

**Scope**:
- [DashboardPage.razor](backend/PriorityHub.Ui/Components/Pages/DashboardPage.razor) — first-run detection and redirect, collapsible hero panel and filter/title panel.
- [Home.razor](backend/PriorityHub.Ui/Components/Pages/Home.razor) — first-run detection trigger point.
- [SettingsPage.razor](backend/PriorityHub.Ui/Components/Pages/SettingsPage.razor) — toast on first-run arrival, import auto-save, tab navigation after import.
- Related CSS files as needed for collapse/expand transitions.

**Audience**: Coding agents implementing the changes, QA agents verifying the changes.

**Assumptions**:
- The existing `IConfigStore` interface and `ProviderConfiguration` model are stable and unchanged.
- The existing inline `_statusMessage` banner in `SettingsPage.razor` is sufficient for the toast notification (no new toast library required).
- `localStorage` is available for persisting collapse state (already used by `HelpPanel` component).
- Blazor Server `InteractiveServer` render mode is available on both `DashboardPage` and `SettingsPage`.

## 2. Definitions

| Term | Definition |
|---|---|
| **Connector** | A configured provider connection (Azure DevOps, GitHub, Jira, Trello, etc.) stored in `ProviderConfiguration`. |
| **First-run user** | An authenticated user whose `ProviderConfiguration` contains zero connections across all provider lists. |
| **Hero panel** | The top section of `DashboardPage.razor` containing the title, description, refresh button, progress bar, and four metric cards (`.hero-panel`). |
| **Title/filter panel** | The left-side panel in the content grid containing the "Unified work queue" heading, help panels, filter controls, and live connectors section (`.filters-panel`). |
| **Import** | The process of uploading a JSON configuration file via the Import/Export tab on the Settings page. |
| **Auto-save** | Automatically calling `SaveConfiguration()` after import confirmation without requiring a manual save click. |
| **Toast** | An inline status banner (`_statusMessage`) displayed at the top of the Settings page. |

## 3. Requirements, Constraints & Guidelines

### Feature 1: First-Run Redirect to Settings

- **REQ-001**: After login, when the dashboard loads and determines the user's configuration has zero enabled connector instances, the system shall redirect the user to `/settings`.
- **REQ-002**: The redirect shall occur after configuration has been loaded (not during the loading spinner phase) and only when all provider connection lists are empty.
- **REQ-003**: When the Settings page is reached via first-run redirect, a toast message shall be displayed: _"Welcome to Priority Hub! Configure at least one connector below to start aggregating your work items."_
- **REQ-004**: The toast message shall use the existing `_statusMessage` / `status-info` banner style (blue, non-error).
- **REQ-005**: The first-run redirect shall only trigger once per page load. If the user navigates back to the dashboard (still with empty config), it shall not redirect again — instead the existing empty-state message in the dashboard is shown.
- **REQ-006**: The redirect shall use a query parameter (e.g., `/settings?onboarding=true`) so the Settings page knows to display the onboarding toast.

- **CON-001**: The redirect must not interfere with the existing OAuth callback flow (`/` → `Home.razor` → `DashboardPage`). The config check runs inside `DashboardPage.OnInitializedAsync` after successful auth.
- **CON-002**: The redirect must not trigger for users who have connectors configured but all are disabled — only when zero connections exist at all.

- **GUD-001**: Keep the detection logic in `DashboardPage`, not in `Home.razor` or `MainLayout`, to avoid adding config-loading concerns to layout rendering.

### Feature 2: Import Auto-Save and Connectors Tab Navigation

- **REQ-010**: After the user clicks "Confirm import" and `ApplyImportAsync()` completes, the system shall automatically call `SaveConfiguration()` to persist the imported settings.
- **REQ-011**: After auto-save completes successfully, the system shall switch the active tab to `"connectors"` so the user can review the imported connections.
- **REQ-012**: If auto-save fails (validation or persistence error), the system shall remain on the Import/Export tab and display the error in the existing status banner.
- **REQ-013**: The status banner after successful import + auto-save shall read: _"Configuration imported and saved. Review your connectors below."_

- **CON-003**: The existing manual save flow must remain functional for regular edits — auto-save is triggered only by the import action.
- **CON-004**: Import validation (preview, masked secret warnings) must still occur before the auto-save step. The auto-save happens only after the user explicitly confirms the import.

### Feature 3: Collapsible Dashboard Panels

- **REQ-020**: The hero panel (`.hero-panel`) shall be collapsible via a toggle button.
- **REQ-021**: The title/filter panel header and filter controls (`.filters-panel` content above the item list) shall be collapsible via a toggle button.
- **REQ-022**: Collapse state for each panel shall be persisted in `localStorage` using keys following the existing `HelpPanel` pattern: `phub:collapse:hero-panel` and `phub:collapse:filters-panel`.
- **REQ-023**: When collapsed, a panel shall show only a compact header bar with the toggle button and a brief label. All inner content shall be hidden.
- **REQ-024**: The default state for both panels shall be **expanded** (matching current behavior for existing users).
- **REQ-025**: The toggle button shall include `aria-expanded` attribute reflecting the current state.
- **REQ-026**: The collapse/expand transition should be instantaneous (no CSS animation required, but a brief transition is acceptable).

- **CON-005**: When the hero panel is collapsed, metric values are hidden. The user must expand it to view metrics. This is acceptable since the data is available in the work-item list itself.
- **CON-006**: When the filters panel is collapsed, filter controls are hidden. Active filters must remain applied — collapsing is visual only; it does not clear filter state.

- **GUD-002**: Use a chevron icon (▼/▲ or similar) consistent with the existing `compact-connectors-toggle` pattern already on the dashboard.
- **GUD-003**: Place the hero panel toggle at the right edge of the hero panel header row (near the "Priority Hub" eyebrow).
- **GUD-004**: Place the filters panel toggle in the `.panel-header` row next to the "Unified work queue" heading.

## 4. Interfaces & Data Contracts

### 4.1 First-Run Detection

No new backend interface. Detection uses the existing `ProviderConfiguration` model:

```csharp
// Check if any connections exist across all providers
bool hasAnyConnections = config.AzureDevOps.Count > 0
    || config.GitHub.Count > 0
    || config.Jira.Count > 0
    || config.MicrosoftTasks.Count > 0
    || config.OutlookFlaggedMail.Count > 0
    || config.Trello.Count > 0
    || config.ImapFlaggedMail.Count > 0;
```

### 4.2 Settings Page Query Parameter

| Parameter | Type | Description |
|---|---|---|
| `onboarding` | `string` (`"true"`) | When present, triggers the onboarding toast message on the Settings page. |

The Settings page reads this via `NavigationManager.Uri` or a `[SupplyParameterFromQuery]` attribute:

```csharp
[SupplyParameterFromQuery]
public string? Onboarding { get; set; }
```

### 4.3 localStorage Keys for Collapse State

| Key | Values | Default |
|---|---|---|
| `phub:collapse:hero-panel` | `"collapsed"` / `"expanded"` | `"expanded"` |
| `phub:collapse:filters-panel` | `"collapsed"` / `"expanded"` | `"expanded"` |

### 4.4 JS Interop for Collapse Persistence

Reuse or extend the existing `PriorityHub` JS namespace (already used for `downloadFile`, `focusById`):

```javascript
PriorityHub.getCollapseState = (key) => localStorage.getItem(key) || "expanded";
PriorityHub.setCollapseState = (key, state) => localStorage.setItem(key, state);
```

Alternatively, if the existing `HelpPanel` already has generic `localStorage` interop, reuse that approach.

## 5. Acceptance Criteria

### Feature 1: First-Run Redirect

- **AC-001**: Given a newly authenticated user with zero configured connectors, When the dashboard page finishes loading the user's configuration, Then the browser navigates to `/settings?onboarding=true`.
- **AC-002**: Given the Settings page is loaded with `?onboarding=true`, When the page initializes, Then a blue status banner displays _"Welcome to Priority Hub! Configure at least one connector below to start aggregating your work items."_
- **AC-003**: Given a user with at least one connector configured (enabled or disabled), When the dashboard loads, Then no redirect occurs and the dashboard renders normally.
- **AC-004**: Given the user navigates back to `/` after the first-run redirect (still with empty config), Then the dashboard shows its existing empty-state message without redirecting again.

### Feature 2: Import Auto-Save

- **AC-010**: Given the user confirms an import on the Import/Export tab, When `ApplyImportAsync()` completes, Then `SaveConfiguration()` is called automatically and the active tab switches to `"connectors"`.
- **AC-011**: Given the auto-save completes successfully, When the connectors tab is shown, Then the status banner reads _"Configuration imported and saved. Review your connectors below."_
- **AC-012**: Given the auto-save fails due to validation errors, When the error occurs, Then the Import/Export tab remains active and the error message is displayed in the status banner.
- **AC-013**: Given the user manually edits fields and clicks "Save connector configuration", When save is triggered, Then the existing manual save behavior is unchanged.

### Feature 3: Collapsible Panels

- **AC-020**: Given the dashboard is loaded with default state, When the user views the hero panel, Then it is expanded and a collapse toggle button is visible.
- **AC-021**: Given the hero panel is expanded, When the user clicks the collapse toggle, Then the hero panel content is hidden and `localStorage["phub:collapse:hero-panel"]` is set to `"collapsed"`.
- **AC-022**: Given the hero panel is collapsed, When the user clicks the expand toggle, Then the panel content is shown and `localStorage` is updated to `"expanded"`.
- **AC-023**: Given the user collapsed the hero panel in a previous session, When the dashboard loads, Then the hero panel is initially collapsed (state restored from `localStorage`).
- **AC-024**: Given the filters panel is collapsed and active filters are applied, When items are rendered, Then the filters remain applied — only the filter UI is visually hidden.
- **AC-025**: Given either toggle button, When inspected, Then `aria-expanded` reflects the current boolean state.

## 6. Test Automation Strategy

- **Test Levels**: Unit tests for the first-run detection logic and import auto-save flow. UI integration tests are out of scope for this iteration.
- **Frameworks**: MSTest, FluentAssertions (existing project conventions from [PriorityHub.Ui.Tests](backend/PriorityHub.Ui.Tests/)).
- **Test Data Management**: Mock `IConfigStore` returning empty and populated `ProviderConfiguration` instances.
- **CI/CD Integration**: All tests run via `dotnet test PriorityHub.sln` in GitHub Actions.
- **Coverage Requirements**: New logic paths (redirect decision, auto-save after import) must have unit tests.
- **Performance Testing**: Not required for this change.

### Specific Tests

| Test | Description |
|---|---|
| `DashboardPage_RedirectsToSettings_WhenNoConnectorsConfigured` | Mock `IConfigStore` returning empty config → verify `NavigationManager.NavigateTo("/settings?onboarding=true")` is called. |
| `DashboardPage_DoesNotRedirect_WhenConnectorsExist` | Mock `IConfigStore` with at least one connection → verify no redirect. |
| `SettingsPage_ShowsOnboardingToast_WhenQueryParamPresent` | Render `SettingsPage` with `?onboarding=true` → verify status banner text. |
| `SettingsPage_AutoSavesAfterImport` | Trigger `ApplyImportAsync()` → verify `ConfigStore.SaveAsync` is called and active tab switches to `"connectors"`. |
| `SettingsPage_StaysOnExportTab_WhenAutoSaveFails` | Trigger import with invalid data → verify tab remains `"export"` and error shown. |

## 7. Rationale & Context

### First-Run Redirect
Currently, a first-time user lands on an empty dashboard with a small "No live connector instances yet" message buried inside the collapsed connectors section. This is easy to miss. Redirecting to Settings with an instructional toast provides a clear, immediate call-to-action that reduces time-to-value.

### Import Auto-Save
The current two-step flow (confirm import → manually save) is unintuitive. Users expect import to be a complete operation. Auto-saving removes the extra click and eliminates the risk of navigating away before saving, which would lose the imported data. Switching to the Connectors tab gives immediate confirmation that the import succeeded.

### Collapsible Panels
Power users who have already configured connectors and understand the scoring model do not need the hero text, metrics, or help panels on every visit. Collapsing these sections maximizes the visible work-item list, which is the primary interaction surface.

## 8. Dependencies & External Integrations

### Infrastructure Dependencies
- **INF-001**: `localStorage` — Required for persisting collapse state. Already used by `HelpPanel` component, so no new capability is needed.

### Technology Platform Dependencies
- **PLT-001**: Blazor Server `InteractiveServer` render mode — Required for interactive toggle buttons and `IJSRuntime` calls. Already in use on both affected pages.
- **PLT-002**: `NavigationManager` — Required for programmatic redirect. Already injected in `DashboardPage`.

### Data Dependencies
- **DAT-001**: `IConfigStore.LoadAsync` — Used to determine if the user has any configured connectors. Already called during dashboard initialization.

## 9. Examples & Edge Cases

### Edge Case: User Deletes All Connectors

Given a user who previously had connectors but removes them all and saves, When they next visit the dashboard, Then the first-run redirect should fire again because the config is now empty.

However, per **REQ-005**, the redirect only fires once per page load to avoid redirect loops. If the user is already on the dashboard when they return from settings (still empty), the dashboard's existing empty-state message handles it.

### Edge Case: Import with Only Masked Secrets

Given a user imports a config file where all PAT fields contain `********`, When they confirm the import, Then auto-save proceeds but the connectors will likely fail at runtime (masked tokens are not valid). The existing masked-secret warning in the import preview is shown before confirmation. This is existing behavior and unchanged.

### Edge Case: Concurrent Config Changes

Given the user has Settings open in two tabs and imports in one, When auto-save runs, Then it overwrites the stored config (last-write-wins). This matches the existing manual save behavior.

### Example: localStorage Collapse State

```javascript
// User collapses hero panel
localStorage.setItem("phub:collapse:hero-panel", "collapsed");

// On next page load, DashboardPage reads:
var state = localStorage.getItem("phub:collapse:hero-panel"); // "collapsed"
// → render hero panel in collapsed state
```

### Example: First-Run Redirect Flow

```
1. User authenticates via Microsoft OAuth
2. Callback redirects to /
3. Home.razor renders <DashboardPage />
4. DashboardPage.OnInitializedAsync() calls ConfigStore.LoadAsync()
5. Config has 0 connections across all providers
6. NavigationManager.NavigateTo("/settings?onboarding=true")
7. SettingsPage loads, reads ?onboarding=true
8. Displays: "Welcome to Priority Hub! Configure at least one connector..."
9. User adds a connector and saves
10. User navigates to / → dashboard loads normally (has connectors now)
```

## 10. Validation Criteria

1. `dotnet build PriorityHub.sln` succeeds with zero errors.
2. `dotnet test PriorityHub.sln` passes with all new and existing tests green.
3. Manual verification: log in with a fresh user (no config file) → confirm redirect to settings with toast.
4. Manual verification: import a config file → confirm auto-save and tab switch to connectors.
5. Manual verification: collapse hero panel → refresh page → confirm it stays collapsed.
6. Manual verification: collapse filters panel → confirm active filters still apply to the item list.
7. Accessibility: all toggle buttons have correct `aria-expanded` attribute values.

## 11. Related Specifications / Further Reading

- [spec-17: Dashboard UX Improvements](plans/specifications/spec-17-feat-dashboard-ux-improvements-icons-gravatar-move-to-top-drag-i.md) — Previous dashboard UX iteration.
- [spec-46: Connector Instance Emoji Picker](plans/specifications/spec-46-feat-connector-instance-emoji-picker-and-item-display.md) — Emoji picker on connector cards.
- Blazor `NavigationManager` docs: https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/routing
- Blazor `localStorage` interop pattern: https://learn.microsoft.com/en-us/aspnet/core/blazor/state-management
