# Plan: Default "Assigned to Me" Filtering and Project Scoping

**Specification**: [`spec/spec-design-default-assigned-to-me-and-project-scoping.md`](../spec/spec-design-default-assigned-to-me-and-project-scoping.md)  
**Status**: Draft  
**Created**: 2026-04-10

---

## Summary

Ensure every connector defaults to fetching only the current user's assigned items. Three connectors need changes; four are already compliant.

| Connector | Change |
|-----------|--------|
| Azure DevOps | Add `[System.AssignedTo] = @Me` to default WIQL |
| Trello | Add `filterMyCards` checkbox (default ON), post-filter cards by token owner's member ID |
| Jira | Add optional `project` ConfigField, prepend `project = "X" AND` to JQL when set |
| GitHub, Microsoft Tasks, Outlook, IMAP | No changes (already user-scoped) |

---

## Phase 1: Azure DevOps — Add `@Me` to Default WIQL

### Step 1.1 — Update `AzureDevOpsConnector.ConfigFields` default WIQL
- [ ] File: `backend/PriorityHub.Api/Services/Connectors/AzureDevOpsConnector.cs` (line ~39)
- [ ] Change WIQL from:
  ```
  ...Where [System.TeamProject] = @project And [System.State] <> 'Closed'...
  ```
  To:
  ```
  ...Where [System.TeamProject] = @project And [System.AssignedTo] = @Me And [System.State] <> 'Closed'...
  ```

### Step 1.2 — Update `AzureDevOpsConnection.Wiql` model default
- [ ] File: `backend/PriorityHub.Api/Models/ConfigModels.cs` (line ~56)
- [ ] Same WIQL change as Step 1.1

### Step 1.3 — Update `providers.example.json`
- [ ] File: `config/providers.example.json`
- [ ] Update both Azure DevOps example entries with the new default WIQL

### Step 1.4 — Update tests (if any reference old default)
- [ ] Check `backend/PriorityHub.Api.Tests/Connectors/AzureDevOpsConnectorTests.cs`
- [ ] Check `backend/PriorityHub.Api.Tests/Connectors/ConnectorHttpTests.cs`
- [ ] Update any hardcoded WIQL strings that match the old default

### Verification
- [ ] `dotnet build PriorityHub.sln`
- [ ] `dotnet test PriorityHub.sln`
- [ ] Manual: Add new ADO connector → WIQL field contains `@Me`

---

## Phase 2: Trello — Add "Filter to My Cards" Toggle

### Step 2.1 — Add `FilterMyCards` property to `TrelloConnection`
- [ ] File: `backend/PriorityHub.Api/Models/ConfigModels.cs`
- [ ] Add: `public bool FilterMyCards { get; set; } = true;`

### Step 2.2 — Add `filterMyCards` ConfigField to `TrelloConnector`
- [ ] File: `backend/PriorityHub.Api/Services/Connectors/TrelloConnector.cs`
- [ ] Add to `ConfigFields` array:
  ```csharp
  new("filterMyCards", "Only show cards assigned to me", "checkbox", false, "true")
  ```

### Step 2.3 — Implement member filtering in `TrelloConnector.FetchConnectionAsync`
- [ ] File: `backend/PriorityHub.Api/Services/Connectors/TrelloConnector.cs`
- [ ] When `connection.FilterMyCards` is `true`:
  1. Call `GET /tokens/{token}/member?fields=id&key={apiKey}` to resolve token owner
  2. After fetching all cards, filter to only cards where `idMembers` contains the member ID
- [ ] On API failure: log warning, fall back to unfiltered cards
- [ ] When `false`: skip member lookup, return all cards (existing behavior)

### Step 2.4 — Add checkbox InputKind to Settings UI
- [ ] File: `backend/PriorityHub.Ui/Components/Pages/SettingsPage.razor`
- [ ] Add `"checkbox"` branch in the `@foreach (var field in connector.ConfigFields)` rendering loop
- [ ] Render using `toggle-row` CSS class pattern (same as the "Enabled" toggle)
- [ ] Handle both `bool` and `"true"`/`"false"` string values

### Step 2.5 — Handle checkbox defaults in `AddConnection()`
- [ ] File: `backend/PriorityHub.Ui/Components/Pages/SettingsPage.razor` (around line 755)
- [ ] When a field has `InputKind == "checkbox"`, store the `DefaultValue` as a `bool` (convert `"true"` → `true`)

### Step 2.6 — Add Trello tests
- [ ] File: `backend/PriorityHub.Api.Tests/Connectors/ConnectorHttpTests.cs` (or new section in `TrelloConnectorTests.cs`)
- [ ] **T-002**: `FilterMyCards=true` → member API called, only matching cards returned
- [ ] **T-003**: `FilterMyCards=false` → member API NOT called, all cards returned
- [ ] **T-004**: `FilterMyCards=true` + member API returns error → all cards returned
- [ ] **T-005**: Card with no matching member ID → excluded
- [ ] **T-006**: Card with matching member ID → included

### Step 2.7 — Update `providers.example.json`
- [ ] File: `config/providers.example.json`
- [ ] Add `"filterMyCards": true` to Trello example entry

### Verification
- [ ] `dotnet build PriorityHub.sln`
- [ ] `dotnet test PriorityHub.sln`
- [ ] Manual: Add new Trello connector → checkbox "Only show cards assigned to me" checked by default

---

## Phase 3: Jira — Add Optional Project ConfigField

### Step 3.1 — Add `Project` property to `JiraConnection`
- [ ] File: `backend/PriorityHub.Api/Models/ConfigModels.cs`
- [ ] Add: `public string Project { get; set; } = string.Empty;`

### Step 3.2 — Add `project` ConfigField to `JiraConnector`
- [ ] File: `backend/PriorityHub.Api/Services/Connectors/JiraConnector.cs`
- [ ] Add to `ConfigFields` array (after `baseUrl`):
  ```csharp
  new("project", "Project key (optional)", "text", false)
  ```

### Step 3.3 — Implement project scoping in `JiraConnector.FetchConnectionAsync`
- [ ] File: `backend/PriorityHub.Api/Services/Connectors/JiraConnector.cs`
- [ ] Before building the API URL:
  ```csharp
  var effectiveJql = connection.Jql;
  if (!string.IsNullOrWhiteSpace(connection.Project))
  {
      effectiveJql = $"project = \"{connection.Project}\" AND {connection.Jql}";
  }
  ```
- [ ] Use `effectiveJql` in the URL instead of `connection.Jql`
- [ ] Set `boardConnection.ProjectName = connection.Project` when non-empty

### Step 3.4 — Add Jira tests
- [ ] File: `backend/PriorityHub.Api.Tests/Connectors/ConnectorHttpTests.cs` (or `JiraConnectorTests.cs`)
- [ ] **T-007**: `Project = "GROWTH"` → JQL has `project = "GROWTH" AND` prepended
- [ ] **T-008**: `Project = ""` → JQL unchanged
- [ ] **T-009**: `BoardConnection.ProjectName` set from `Project` field

### Step 3.5 — Update `providers.example.json`
- [ ] File: `config/providers.example.json`
- [ ] Add `"project": "GROWTH"` to Jira example entry

### Verification
- [ ] `dotnet build PriorityHub.sln`
- [ ] `dotnet test PriorityHub.sln`
- [ ] Manual: Add new Jira connector → "Project key (optional)" field visible and empty

---

## Phase 4: Documentation & Final Verification

### Step 4.1 — Update CHANGELOG.md
- [ ] File: `CHANGELOG.md`
- [ ] Add under `[Unreleased]` > `Changed`:
  - Azure DevOps connector default WIQL now includes `[System.AssignedTo] = @Me` to filter items assigned to the current user
  - Trello connector now filters cards to only those assigned to the token owner by default (toggle available)
  - Jira connector now supports an optional project key field to scope queries by project

### Step 4.2 — Update docs (if affected)
- [ ] Check `docs/quick-start.md` — update if connector setup instructions are affected
- [ ] Check `docs/configuration/README.md` — update if connector defaults are documented
- [ ] Check `docs/features/README.md` — update if connector features are documented

### Step 4.3 — Final build and test
- [ ] `dotnet build PriorityHub.sln` — zero errors
- [ ] `dotnet test PriorityHub.sln` — all tests pass

---

## Files Modified

| File | Changes |
|------|---------|
| `backend/PriorityHub.Api/Services/Connectors/AzureDevOpsConnector.cs` | Update default WIQL in `ConfigFields` |
| `backend/PriorityHub.Api/Services/Connectors/TrelloConnector.cs` | Add `filterMyCards` field, implement member filtering |
| `backend/PriorityHub.Api/Services/Connectors/JiraConnector.cs` | Add `project` field, implement JQL prepend |
| `backend/PriorityHub.Api/Models/ConfigModels.cs` | Update `AzureDevOpsConnection.Wiql` default, add `TrelloConnection.FilterMyCards`, add `JiraConnection.Project` |
| `backend/PriorityHub.Ui/Components/Pages/SettingsPage.razor` | Add checkbox `InputKind` rendering, handle checkbox defaults |
| `config/providers.example.json` | Update ADO WIQL, add Jira `project`, add Trello `filterMyCards` |
| `backend/PriorityHub.Api.Tests/Connectors/ConnectorHttpTests.cs` | Add Trello member filtering and Jira project scoping HTTP tests |
| `backend/PriorityHub.Api.Tests/Connectors/TrelloConnectorTests.cs` | Add unit tests for member filtering logic |
| `backend/PriorityHub.Api.Tests/Connectors/JiraConnectorTests.cs` | Add unit tests for project prepend |
| `CHANGELOG.md` | Unreleased entry |
