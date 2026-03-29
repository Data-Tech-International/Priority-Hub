# Specification: feat: Add TargetDate & IsBlocked fields with dashboard filters and Connectors multi-select

## Metadata
- Source issue: #44
- Source URL: https://github.com/Data-Tech-International/Priority-Hub/issues/44
- Author: @ipavlovi
- Created: 2026-03-29T20:46:36Z

## Specification

## Summary

Add two new fields to `WorkItem`: `TargetDate` (`DateTimeOffset?`) and `IsBlocked` (`bool`). Display them on dashboard cards with a "days left" countdown and blocked visual treatment. Replace the Provider filter with a multi-select Connectors filter. Add dashboard filters for blocked and target date. Integrate `IsBlocked` into the priority scoring formula.

## Phase 1: Model & Backend (blocking — all other phases depend on this)

- Add `DateTimeOffset? TargetDate` and `bool IsBlocked` to `WorkItem` in `DashboardModels.cs`
- Update all 6 connectors to populate new fields:
  - **Azure DevOps** — `TargetDate` from `Microsoft.VSTS.Scheduling.TargetDate`; `IsBlocked` from status/deps
  - **Jira** — `TargetDate` from `fields.duedate`; `IsBlocked` from status
  - **Trello** — `TargetDate` from `card.due`; `IsBlocked` from "blocked" label
  - **Microsoft Tasks** — `TargetDate` from `dueDateTime`; `IsBlocked` from "waitingOnOthers"
  - **GitHub Issues** — `TargetDate = null`; `IsBlocked` from "blocked" label
  - **Outlook Flagged Mail** — both stay at defaults
- Connectors without source data keep default values (`null` / `false`)

## Phase 2: Priority Formula (depends on Phase 1)

- Add `+ (item.IsBlocked ? 6 : 0)` to scoring formula in `WorkItemRanker`
- `TargetDate` already covered by existing `DueInDays` → `DueDateWeight()` path — no separate weight needed

## Phase 3: Dashboard UI (depends on Phase 1, parallel with Phase 2)

### 3a: Replace Provider filter with multi-select Connectors filter
- **Replace Provider single-select dropdown** with a new `ConnectorFilter` component (mirrors `TagFilter` pattern)
  - Remove `_selectedProvider` state and `OnProviderChanged` handler; remove static `ProviderOptions` array
  - Add `_selectedConnectors` (`List<string>`) + `OnConnectorsChanged` callback
  - Available items: distinct `Provider` values from `_dashboard.BoardConnections`, displayed via `FormatProviderName`
  - Empty selection = show all; non-empty = item matches ANY selected connector

### 3b: Create `ConnectorFilter.razor` component (new)
- Multi-select dropdown with checkboxes, outside-click JS interop, keyboard navigation, clear button
- Parameters: `AvailableConnectors`, `SelectedConnectors`, `OnConnectorsChange`
- Display names via `WorkItemRanker.FormatProviderName()`
- Label: "All connectors" / formatted name / "Connectors (N)"

### 3c: Add Blocked filter
- Dropdown: "All" / "Blocked only" / "Not blocked"

### 3d: Add Target date filter
- Dropdown: "All" / "Has target date" / "No target date" / "Overdue" / "Due within 7 days"

### 3e: Display on item cards
- New metadata row between title and tags:
  - If `TargetDate` set: formatted date + "X days left" / "Overdue by X days" badge
  - If `IsBlocked`: "Blocked" pill (red/orange, similar to "New item" pill)
- Add `.is-blocked` CSS class to card when `IsBlocked = true`
- Helper method `FormatDaysLeft(DateTimeOffset?)` for display text

### 3f: CSS styles
- `.work-item-card.is-blocked`: subtle warm background tint
- `.blocked-pill`: red/orange pill
- `.target-date-info`: metadata row
- `.days-left-badge`: color-coded (red overdue, amber ≤7 days, neutral otherwise)
- Connector filter styles (matching tag filter pattern)

## Phase 4: Tests (parallel with Phase 3)

- **WorkItemRankerTests** — update `MakeItem` helper, add `Rank_BlockedItem_ScoresHigherThanUnblocked`, `Rank_BlockedItem_BandAssignment`
- **Connector tests** — verify `TargetDate` and `IsBlocked` per connector
- **DashboardAggregatorTests** — update if `MakeItem` needs new defaults
- **ConnectorFilterTests** (new) — bUnit tests following `TagFilterTests` pattern

## Phase 5: Documentation (depends on all above)

- **CHANGELOG.md** under `[Unreleased]`:
  - Added: TargetDate and IsBlocked fields, dashboard filters, blocked styling, days-left countdown
  - Changed: Provider filter → multi-select Connectors filter, scoring formula includes IsBlocked boost
- **docs/features/README.md** — document new fields, filters, visual behavior

## Key Decisions

| Decision | Rationale |
|---|---|
| IsBlocked scoring: +6 boost | Moderate; surfaces blocked items without dominating. Complements existing `BlockerCount × 4`. |
| TargetDate: no separate formula weight | Already captured by `DueInDays → DueDateWeight()` that connectors populate from the same dates. |
| ConnectorFilter: dynamic from live data | List comes from `BoardConnections`, not hardcoded. Multi-select with ANY-match semantics. |
| Unsupported sources: safe defaults | `TargetDate = null`, `IsBlocked = false` — no errors for connectors without these fields. |
| Scope: read-only from sources | No UI editing of these fields. MINOR version increment. |

## Verification

1. `dotnet build PriorityHub.sln` — clean build
2. `dotnet test PriorityHub.sln` — all existing + new tests pass
3. Manual: blocked items show tinted background + "Blocked" pill
4. Manual: target date shows formatted date with "X days left" countdown
5. Manual: Connectors multi-select filter correctly combines selections
6. Manual: Blocked and Target date filters work correctly
7. Manual: connectors without source data show defaults without errors

## Clarifications
- [ ] Confirm assumptions before planning if anything is unclear.
