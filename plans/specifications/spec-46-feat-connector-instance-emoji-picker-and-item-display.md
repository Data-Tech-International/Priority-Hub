# Specification: feat: Connector instance emoji picker and item display

## Metadata
- Source issue: #46
- Source URL: https://github.com/Data-Tech-International/Priority-Hub/issues/46
- Author: @ipavlovi
- Created: 2026-03-29T21:43:09Z

## Specification

## Summary

Add a user-selectable Unicode emoji to every connector instance so that work items from different data sources are instantly distinguishable in the dashboard, connector cards, filters, and settings.

## Key Points

- **Emoji scope**: per connector **instance** (each connection gets its own emoji)
- **Defaults**: sensible per-type — 🔷 ADO, 🐙 GitHub, 📋 Jira, 📌 Trello, ✅ Tasks, 📧 Outlook
- **Picker**: pure C#/Blazor `EmojiPicker` component with categorised groups and keyword search (no JS library)
- **Display**: 24 px emoji before item title, on connector cards, settings headers, and filter dropdowns
- **Persistence**: `"emoji"` field in connection JSON config; missing field falls back to type default

## Display Locations

1. Work-item cards — emoji before title
2. Connector status cards — emoji before board name
3. Settings page — connector section headers and connection editor cards
4. Provider filter dropdown (current) — emoji before provider name
5. Connectors multi-select filter (upcoming) — emoji before connection name

## Specification

Full specification: [`plans/specifications/spec-design-connector-emoji-picker-and-item-display.md`](plans/specifications/spec-design-connector-emoji-picker-and-item-display.md)

## Acceptance Criteria

- New connections default to the connector type's emoji
- Emoji picker opens, searches, selects, and persists
- Every work item card shows source emoji at 24 px before title
- Connector cards show emoji before board name
- Filter dropdowns show emojis
- Legacy config without `"emoji"` loads with defaults (no errors)
- Config round-trip preserves custom emoji selections
- Validation rejects arbitrary text in the emoji field

## Clarifications
- [ ] Confirm assumptions before planning if anything is unclear.
