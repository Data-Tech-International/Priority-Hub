---
title: Default "Assigned to Me" Filtering and Project Scoping for All Connectors
version: 1.0
date_created: 2026-04-10
last_updated: 2026-04-10
owner: Priority Hub Team
tags: design, connectors, ux, configuration, defaults
---

# Introduction

This specification defines changes to Priority Hub's connector configuration defaults so that every newly added connector instance fetches only work items **assigned to the current user** and, where applicable, **scoped to a specific project**. The goal is a "useful out of the box" experience: when a user adds a connector, the default query or filter should return their personal work — not the entire board, project, or organization.

## 1. Purpose & Scope

**Purpose**: Ensure that all connector types default to fetching only the authenticated user's assigned items, and that project/board scoping is available and pre-filled where the external system supports it.

**Scope**:
- Modify default query/filter values for connectors that currently lack "assigned to me" defaults.
- Add new configuration fields where needed (Trello member filter toggle, Jira project field).
- Add a `checkbox` `InputKind` to the Settings UI for boolean configuration fields.
- Update `ProviderConfiguration` model classes, connector `ConfigFields` metadata, and `providers.example.json`.
- Only **new** connector instances are affected. Existing saved configurations are not modified.

**Intended audience**: Coding agents implementing the feature; reviewers validating acceptance criteria.

**Assumptions**:
- Users prefer a focused view of their own work items by default.
- Users can always override defaults by editing the query/filter fields after adding a connector.
- The Trello REST API returns `idMembers` on cards and supports resolving the token owner via `/tokens/{token}/member`.
- Azure DevOps WIQL supports the `@Me` macro (resolved server-side based on the authenticated identity).

**Out of scope**:
- Migration of existing saved connector configurations.
- Changes to Outlook Flagged Mail, Microsoft Tasks, or IMAP Flagged Mail connectors (these already fetch only the authenticated user's own data by design).
- Changes to the GitHub Issues connector (default query is already `is:open assignee:@me`).

## 2. Definitions

| Term | Definition |
|---|---|
| **Connector** | An `IConnector` implementation that fetches work items from an external source (`azure-devops`, `github`, `jira`, `trello`, `microsoft-tasks`, `outlook-flagged-mail`, `imap-flagged-mail`). |
| **Connector Instance** | A single configured connection within a connector type (e.g., one Azure DevOps connection targeting a specific project). |
| **ConfigFields** | The `ConnectorFieldSpec[]` array on each `IConnector` that defines the configuration fields rendered in the Settings UI, including their keys, labels, input types, required status, and default values. |
| **ConnectorFieldSpec** | A record: `ConnectorFieldSpec(string Key, string Label, string InputKind, bool Required, string? DefaultValue)`. |
| **InputKind** | The type of HTML input rendered in the Settings UI for a config field. Current values: `"text"`, `"textarea"`, `"password"`. This spec adds `"checkbox"`. |
| **WIQL** | Work Item Query Language — Azure DevOps query syntax. Supports macros like `@project` (current project) and `@Me` (authenticated user). |
| **JQL** | Jira Query Language — Jira search syntax. Supports functions like `currentUser()` for the authenticated user and `project = KEY` for project scoping. |
| **`@Me`** | A WIQL macro resolved server-side by the Azure DevOps API to the identity of the authenticated user making the request. |
| **Token Owner** | The Trello user who generated the API token. Resolved via `GET /tokens/{token}/member`. |

## 3. Requirements, Constraints & Guidelines

### Azure DevOps — Add `@Me` to Default WIQL

- **REQ-001**: The default WIQL in `AzureDevOpsConnector.ConfigFields` MUST include `[System.AssignedTo] = @Me` in the `Where` clause.
- **REQ-002**: The default value of `AzureDevOpsConnection.Wiql` in the model class MUST match the `ConfigFields` default exactly.
- **REQ-003**: The updated default WIQL MUST be: `Select [System.Id] From WorkItems Where [System.TeamProject] = @project And [System.AssignedTo] = @Me And [System.State] <> 'Closed' Order By [System.ChangedDate] Desc`.
- **REQ-004**: The `providers.example.json` file MUST be updated so that all Azure DevOps example entries use the new default WIQL containing `@Me`.

### Trello — Add "Filter to My Cards" Toggle

- **REQ-010**: `TrelloConnector.ConfigFields` MUST include a new field with key `filterMyCards`, label `"Only show cards assigned to me"`, `InputKind = "checkbox"`, `Required = false`, and `DefaultValue = "true"`.
- **REQ-011**: `TrelloConnection` model MUST include a property `FilterMyCards` of type `bool` with a default value of `true`.
- **REQ-012**: When `FilterMyCards` is `true`, the connector MUST resolve the token owner's Trello member ID by calling `GET https://api.trello.com/1/tokens/{token}/member?fields=id&key={apiKey}` and MUST include only cards where the `idMembers` JSON array contains the resolved member ID.
- **REQ-013**: When `FilterMyCards` is `false`, the connector MUST return all cards from the board (current behavior).
- **REQ-014**: If the member ID resolution API call fails (network error, invalid token, non-200 response), the connector MUST log a warning, fall back to returning all cards (unfiltered), and MUST NOT add an error to `ConnectorResult.Issues` for the member resolution failure alone.
- **REQ-015**: The member ID resolution API call MUST use the same `HttpClient` instance and authentication parameters (`apiKey`, `token`) as the existing board API calls.
- **REQ-016**: The member ID resolution MUST occur before the cards fetch, so that the member ID is available for post-filtering after cards are retrieved.

### Jira — Add Optional Project Field

- **REQ-020**: `JiraConnector.ConfigFields` MUST include a new field with key `project`, label `"Project key (optional)"`, `InputKind = "text"`, `Required = false`, and no default value (empty string).
- **REQ-021**: `JiraConnection` model MUST include a property `Project` of type `string` with a default value of `string.Empty`.
- **REQ-022**: When `JiraConnection.Project` is a non-empty, non-whitespace string, the connector MUST prepend `project = "{value}" AND ` to the user's configured JQL before sending the API request.
- **REQ-023**: When `JiraConnection.Project` is empty or whitespace, the connector MUST use the JQL as-is without modification.
- **REQ-024**: When `JiraConnection.Project` is non-empty, the `BoardConnection.ProjectName` MUST be set to the `Project` value instead of the `BaseUrl` host.
- **REQ-025**: The `providers.example.json` file MUST be updated to include a `"project"` field in the Jira example entries.

### Settings UI — Checkbox InputKind

- **REQ-030**: The Settings page field rendering logic MUST support `InputKind = "checkbox"`, rendering an `<input type="checkbox">` element with a label.
- **REQ-031**: The checkbox MUST be rendered using the existing `toggle-row` CSS class pattern already used for the "Enabled" toggle, placed inline within the `config-grid` layout.
- **REQ-032**: The checkbox checked state MUST be bound to the connection field value. A checked checkbox corresponds to the string value `"true"` (or boolean `true`); unchecked corresponds to `"false"` (or boolean `false`).
- **REQ-033**: When `AddConnection()` creates a new connection dictionary, the `DefaultValue` of a checkbox field (`"true"` or `"false"`) MUST be stored as a boolean (`true` / `false`), consistent with how the `enabled` field is stored.

### Existing Connectors — No Changes Required (Verification Only)

- **REQ-040**: GitHub Issues connector default query MUST remain `"is:open assignee:@me"` (already compliant).
- **REQ-041**: Microsoft Tasks connector MUST continue using the `/me/todo/lists` Graph API endpoint (inherently user-scoped).
- **REQ-042**: Outlook Flagged Mail connector MUST continue using the `/me/messages` Graph API endpoint (inherently user-scoped).
- **REQ-043**: IMAP Flagged Mail connector MUST continue fetching from the authenticated user's mailbox (inherently user-scoped).

### Backward Compatibility

- **REQ-050**: Existing saved connector configurations MUST NOT be modified. Changes apply only to default values used when creating new connector instances via the Settings UI.
- **REQ-051**: The `TrelloConnection.FilterMyCards` property MUST default to `true` when deserialized from existing JSON that does not contain the `filterMyCards` field. This means existing Trello connections will gain the filter behavior on their next fetch. If this is undesirable, the default MUST be `false` and only new connections get `true` via `ConfigFields.DefaultValue`.
- **REQ-052**: The `JiraConnection.Project` property MUST default to `string.Empty` when deserialized from existing JSON that does not contain the `project` field. This preserves existing behavior (JQL used as-is).

### Security

- **SEC-001**: The Trello member resolution API call MUST NOT log or expose the `token` value in log messages. Use the same logging pattern as existing Trello connector calls.
- **SEC-002**: The Jira project value MUST be URI-encoded when included in the JQL query string to prevent injection.

### Constraints

- **CON-001**: The checkbox `InputKind` rendering MUST reuse existing CSS classes. No new CSS files or classes are introduced.
- **CON-002**: The Trello member ID resolution adds one additional HTTP request per Trello connector fetch when `FilterMyCards` is `true`. This is acceptable for the intended use case.
- **CON-003**: The Jira project prepend to JQL is syntactically redundant if the user's JQL already contains a `project =` clause. This is harmless and the API handles it correctly.

### Guidelines

- **GUD-001**: Place all changes in the existing files. No new files are required except test files.
- **GUD-002**: Follow existing connector patterns: `ConfigFields` defines UI metadata and defaults; model classes in `ConfigModels.cs` define property defaults; connectors read from deserialized model instances.
- **GUD-003**: Use the `QueuedHttpMessageHandler` and `CapturingHttpMessageHandler` test helpers from `ConnectorHttpTests.cs` for new HTTP-level tests.

### Patterns

- **PAT-001**: Trello member filtering follows the post-filter pattern: fetch all cards, then filter in memory. This keeps the API call simple and consistent with the existing board cards endpoint.
- **PAT-002**: Jira project scoping follows the query-prepend pattern: the connector modifies the JQL string before sending it to the API. This is similar to how `@project` works in Azure DevOps WIQL.

## 4. Interfaces & Data Contracts

### 4.1 Azure DevOps — Updated Default WIQL

**`AzureDevOpsConnector.ConfigFields`** (updated `wiql` field default):
```
Select [System.Id] From WorkItems Where [System.TeamProject] = @project And [System.AssignedTo] = @Me And [System.State] <> 'Closed' Order By [System.ChangedDate] Desc
```

**`AzureDevOpsConnection.Wiql`** property default — same value as above.

### 4.2 Trello — New ConfigField and Model Property

**New `ConnectorFieldSpec`** added to `TrelloConnector.ConfigFields`:
```csharp
new("filterMyCards", "Only show cards assigned to me", "checkbox", false, "true")
```

**New property on `TrelloConnection`**:
```csharp
public bool FilterMyCards { get; set; } = true;
```

**Trello Member Resolution API Call**:
```
GET https://api.trello.com/1/tokens/{token}/member?fields=id&key={apiKey}
```

Response (JSON):
```json
{
  "id": "5a1234567890abcdef012345"
}
```

**Card filtering logic** (pseudocode):
```
if FilterMyCards:
    myMemberId = GET /tokens/{token}/member?fields=id → response.id
    cards = GET /boards/{boardId}/cards (existing call)
    filteredCards = cards.Where(card => card.idMembers.Contains(myMemberId))
else:
    filteredCards = GET /boards/{boardId}/cards (existing call, no filter)
```

### 4.3 Jira — New ConfigField and Model Property

**New `ConnectorFieldSpec`** added to `JiraConnector.ConfigFields` (after `baseUrl`):
```csharp
new("project", "Project key (optional)", "text", false)
```

**New property on `JiraConnection`**:
```csharp
public string Project { get; set; } = string.Empty;
```

**JQL Prepend Logic** (pseudocode):
```
effectiveJql = connection.Jql
if !string.IsNullOrWhiteSpace(connection.Project):
    effectiveJql = $"project = \"{connection.Project}\" AND {connection.Jql}"
```

### 4.4 Settings UI — Checkbox InputKind

New rendering branch in `SettingsPage.razor` field loop:

```razor
@if (field.InputKind == "checkbox")
{
    <label class="toggle-row">
        <input type="checkbox"
               checked="@(IsCheckboxChecked(connection, field.Key))"
               @onchange="e => SetConnectionField(configKey, index, field.Key, e.Value)" />
        <span>@field.Label</span>
    </label>
}
```

Where `IsCheckboxChecked` reads the field value and returns `true` when it equals `true` (bool) or `"true"` (string).

### 4.5 Updated providers.example.json

Azure DevOps entries:
```json
{
  "wiql": "Select [System.Id] From WorkItems Where [System.TeamProject] = @project And [System.AssignedTo] = @Me And [System.State] <> 'Closed' Order By [System.ChangedDate] Desc"
}
```

Jira entry:
```json
{
  "project": "GROWTH",
  "jql": "assignee = currentUser() AND statusCategory != Done ORDER BY priority DESC, updated DESC"
}
```

Trello entry (unchanged schema; `filterMyCards` defaults to `true` via model property):
```json
{
  "id": "trello-consulting",
  "name": "Advisory Pipeline",
  "boardId": "replace-with-board-id",
  "apiKey": "replace-with-key",
  "token": "replace-with-token",
  "filterMyCards": true,
  "enabled": true
}
```

## 5. Acceptance Criteria

- **AC-001**: Given a user adds a new Azure DevOps connector via the Settings UI, When the connection is created, Then the WIQL field is pre-populated with `Select [System.Id] From WorkItems Where [System.TeamProject] = @project And [System.AssignedTo] = @Me And [System.State] <> 'Closed' Order By [System.ChangedDate] Desc`.
- **AC-002**: Given a user adds a new Trello connector via the Settings UI, When the connection is created, Then a checkbox labeled "Only show cards assigned to me" is visible and checked by default.
- **AC-003**: Given a Trello connector with `FilterMyCards = true`, When the connector fetches cards, Then only cards where `idMembers` contains the token owner's member ID are returned.
- **AC-004**: Given a Trello connector with `FilterMyCards = false`, When the connector fetches cards, Then all cards from the board are returned (current behavior).
- **AC-005**: Given a Trello connector with `FilterMyCards = true` and the member resolution API call fails, When the connector fetches cards, Then all cards are returned with a warning logged (graceful fallback).
- **AC-006**: Given a user adds a new Jira connector via the Settings UI, When the connection is created, Then a "Project key (optional)" text field is visible and empty by default.
- **AC-007**: Given a Jira connector with `Project = "GROWTH"`, When the connector fetches issues, Then the JQL sent to the API is `project = "GROWTH" AND assignee = currentUser() AND statusCategory != Done ORDER BY updated DESC`.
- **AC-008**: Given a Jira connector with `Project` empty, When the connector fetches issues, Then the JQL is sent as configured (no prepend).
- **AC-009**: Given an existing saved Azure DevOps connector configuration without `@Me` in its WIQL, When the user loads the Settings page, Then the WIQL field shows the user's saved value (not the new default).
- **AC-010**: Given an existing saved Trello connector configuration without a `filterMyCards` field, When the connector is deserialized, Then `FilterMyCards` defaults to `true` and the filter is active.
- **AC-011**: Given an existing saved Jira connector configuration without a `project` field, When the connector is deserialized, Then `Project` defaults to `string.Empty` and the JQL is used as-is.
- **AC-012**: The system SHALL render checkbox fields in the Settings UI using the `toggle-row` CSS class pattern, consistent with the existing "Enabled" toggle.

## 6. Test Automation Strategy

- **Test Levels**: Unit tests for connector logic changes; no integration or E2E tests required.
- **Frameworks**: MSTest, FluentAssertions (if in use), existing test helpers (`QueuedHttpMessageHandler`, `CapturingHttpMessageHandler`, `TestHelpers.JsonOf()`).
- **Test Data Management**: JSON fixtures embedded in test code via anonymous objects and `ConnectionJson()` helper.
- **CI/CD Integration**: All tests run via `dotnet test PriorityHub.sln` in existing CI pipeline.
- **Coverage Requirements**: All new code paths (Trello member filtering ON/OFF/fallback, Jira project prepend, checkbox field handling) must have dedicated test cases.

### Required Test Cases

| ID | Test | File |
|---|---|---|
| **T-001** | ADO default WIQL contains `@Me` | `AzureDevOpsConnectorTests.cs` or `ConnectorHttpTests.cs` |
| **T-002** | Trello `FilterMyCards=true` → member API called, cards filtered by member ID | `ConnectorHttpTests.cs` |
| **T-003** | Trello `FilterMyCards=false` → member API NOT called, all cards returned | `ConnectorHttpTests.cs` |
| **T-004** | Trello `FilterMyCards=true` + member API failure → all cards returned, warning logged | `ConnectorHttpTests.cs` |
| **T-005** | Trello `FilterMyCards=true` + card has no `idMembers` matching → card excluded | `ConnectorHttpTests.cs` |
| **T-006** | Trello `FilterMyCards=true` + card has matching member ID → card included | `ConnectorHttpTests.cs` |
| **T-007** | Jira `Project` non-empty → JQL has `project = "X" AND` prepended | `JiraConnectorTests.cs` or `ConnectorHttpTests.cs` |
| **T-008** | Jira `Project` empty → JQL unchanged | `JiraConnectorTests.cs` or `ConnectorHttpTests.cs` |
| **T-009** | Jira `Project` sets `BoardConnection.ProjectName` | `ConnectorHttpTests.cs` |
| **T-010** | GitHub default query still contains `assignee:@me` (regression guard) | `GitHubIssuesConnectorTests.cs` |

## 7. Rationale & Context

Priority Hub aggregates work items from multiple external services into a single dashboard. The core value proposition is a **personal** prioritized view of a user's work. When a user adds a new connector, the default configuration should immediately deliver this personal view without manual query editing.

Currently, three of seven connectors do not default to user-scoped filtering:

1. **Azure DevOps**: Default WIQL returns all non-closed items in the project. Users must manually add `[System.AssignedTo] = @Me` to see only their own work.
2. **Trello**: Fetches all cards from a board regardless of assignment. There is no query syntax — filtering must be done programmatically.
3. **Jira**: Default JQL already filters by `assignee = currentUser()`, but has no project field, so the user must embed the project key in the JQL manually.

The remaining four connectors (GitHub, Microsoft Tasks, Outlook, IMAP) either include `assignee:@me` in their default query or are inherently user-scoped via their API design.

## 8. Dependencies & External Integrations

### External Systems
- **EXT-001**: Azure DevOps REST API v7.1 — WIQL endpoint resolves `@Me` macro based on the authenticated identity (PAT or Bearer token).
- **EXT-002**: Trello REST API v1 — `GET /tokens/{token}/member` returns the member ID of the token owner. `GET /boards/{boardId}/cards` returns `idMembers` array per card.
- **EXT-003**: Jira REST API v3 — `GET /rest/api/3/search/jql` accepts `project = "KEY" AND ...` in JQL. `currentUser()` function resolves from Basic auth credentials.

### Technology Platform Dependencies
- **PLT-001**: .NET 10 — Target framework for all production and test code.
- **PLT-002**: Blazor Server — Settings page renders `ConnectorFieldSpec` metadata dynamically; new `checkbox` InputKind must be supported in Razor rendering.

## 9. Examples & Edge Cases

### Azure DevOps — `@Me` Behavior

```sql
-- New default WIQL:
Select [System.Id]
From WorkItems
Where [System.TeamProject] = @project
  And [System.AssignedTo] = @Me
  And [System.State] <> 'Closed'
Order By [System.ChangedDate] Desc
```

**Edge case**: If the user authenticates with a PAT that has limited scope, `@Me` resolves to the PAT owner. If using Microsoft OAuth Bearer token, `@Me` resolves to the signed-in Entra ID user.

**Edge case**: If a work item has no assignee, `[System.AssignedTo] = @Me` will exclude it. This is the intended behavior — unassigned items are not the user's responsibility.

### Trello — Member Filtering

```json
// API response: GET /tokens/{token}/member?fields=id
{ "id": "5a1234567890abcdef012345" }

// Card with matching member:
{ "id": "card1", "name": "Fix bug", "idMembers": ["5a1234567890abcdef012345", "other-id"] }
// → INCLUDED (user is assigned)

// Card without matching member:
{ "id": "card2", "name": "Design review", "idMembers": ["other-id"] }
// → EXCLUDED (user is not assigned)

// Card with empty members:
{ "id": "card3", "name": "Backlog item", "idMembers": [] }
// → EXCLUDED (no one assigned)
```

**Edge case**: A card with `idMembers` containing multiple members — included as long as the token owner is one of them.

**Edge case**: Member API returns HTTP 401 (expired token) — log warning, return all cards unfiltered.

### Jira — Project Scoping

```
// User's configured JQL:
assignee = currentUser() AND statusCategory != Done ORDER BY updated DESC

// With Project = "GROWTH":
project = "GROWTH" AND assignee = currentUser() AND statusCategory != Done ORDER BY updated DESC

// With Project = "" (empty):
assignee = currentUser() AND statusCategory != Done ORDER BY updated DESC
```

**Edge case**: User's JQL already contains `project = GROWTH`. The prepended clause becomes `project = "GROWTH" AND project = GROWTH AND ...`. Jira treats this as redundant and returns correct results.

**Edge case**: Project key with special characters (e.g., spaces) — the value is URI-encoded in the query string and quoted in the JQL.

### Settings UI — Checkbox Rendering

```razor
@* Checkbox field renders as a toggle row within the config grid *@
<label class="toggle-row">
    <input type="checkbox" checked="@true" @onchange="..." />
    <span>Only show cards assigned to me</span>
</label>
```

**Edge case**: Existing connection JSON with `"filterMyCards": true` (boolean) vs. `"filterMyCards": "true"` (string) — both must be handled by `IsCheckboxChecked()`.

## 10. Validation Criteria

1. `dotnet build PriorityHub.sln` completes without errors.
2. `dotnet test PriorityHub.sln` — all existing and new tests pass.
3. New Azure DevOps connector instance has WIQL containing `@Me`.
4. New Trello connector instance has "Only show cards assigned to me" checkbox checked.
5. New Jira connector instance has "Project key (optional)" field visible and empty.
6. Existing saved connector configurations load with their original values (no mutation).
7. `providers.example.json` reflects updated defaults for Azure DevOps and Jira.
8. `CHANGELOG.md` has an entry under `[Unreleased]` > `Changed`.

## 11. Related Specifications / Further Reading

- [Azure DevOps WIQL syntax](https://learn.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax) — `@Me` and `@project` macro reference
- [Trello REST API — Get Token Member](https://developer.atlassian.com/cloud/trello/rest/api-group-tokens/#api-tokens-token-member-get) — resolves token owner
- [Trello REST API — Get Cards on a Board](https://developer.atlassian.com/cloud/trello/rest/api-group-boards/#api-boards-id-cards-get) — `idMembers` field
- [Jira JQL syntax](https://support.atlassian.com/jira-software-cloud/docs/what-is-advanced-searching-in-jira-software/) — `project` clause and `currentUser()` function
- [Priority Hub connector architecture](backend/PriorityHub.Api/Services/Connectors/IConnector.cs) — `IConnector` interface and `ConnectorFieldSpec` record
