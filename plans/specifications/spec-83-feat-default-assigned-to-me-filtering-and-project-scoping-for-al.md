# Specification: feat: Default 'assigned to me' filtering and project scoping for all connectors

## Metadata
- Source issue: #83
- Source URL: https://github.com/Data-Tech-International/Priority-Hub/issues/83
- Author: @ipavlovi
- Created: 2026-04-10T20:20:03Z

## Specification

## Summary

Ensure every connector type defaults to fetching only work items **assigned to the current user** and, where applicable, **scoped to a specific project** when a new connector instance is added.

## Motivation

Priority Hub's core value is a personal prioritized view of a user's work. Three of seven connectors currently do not default to user-scoped filtering:

- **Azure DevOps**: Default WIQL returns all non-closed items in the project (no `@Me` filter)
- **Trello**: Fetches all cards from a board regardless of assignment
- **Jira**: Defaults to `assignee = currentUser()` but has no project field

## Changes

| Connector | Change |
|-----------|--------|
| Azure DevOps | Add `[System.AssignedTo] = @Me` to default WIQL |
| Trello | Add `filterMyCards` checkbox (default ON) — resolves token owner via `/tokens/{token}/member`, post-filters cards by `idMembers` |
| Jira | Add optional `project` text field — prepends `project = "X" AND` to JQL when set |
| GitHub, Microsoft Tasks, Outlook, IMAP | No changes (already user-scoped) |
| Settings UI | Add `checkbox` InputKind support for boolean config fields |

## Specification

[`spec/spec-design-default-assigned-to-me-and-project-scoping.md`](https://github.com/Data-Tech-International/Priority-Hub/blob/main/spec/spec-design-default-assigned-to-me-and-project-scoping.md)

## Plan

[`plans/default-assigned-to-me-scoping.md`](https://github.com/Data-Tech-International/Priority-Hub/blob/main/plans/default-assigned-to-me-scoping.md)

## Acceptance Criteria

- [ ] New Azure DevOps connector WIQL includes `@Me`
- [ ] New Trello connector has "Only show cards assigned to me" checkbox (default ON)
- [ ] Trello member filtering works (ON/OFF/fallback on API error)
- [ ] New Jira connector has optional "Project key" field
- [ ] Jira project scoping prepends to JQL when set
- [ ] Settings UI renders checkbox InputKind fields
- [ ] Existing saved connector configs are not modified
- [ ] `providers.example.json` updated
- [ ] All tests pass
- [ ] CHANGELOG.md updated

## Clarifications
- [ ] Confirm assumptions before planning if anything is unclear.
