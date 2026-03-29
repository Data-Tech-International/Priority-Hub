---
title: Connector Instance Emoji Picker and Item Display
version: 1.0
date_created: 2026-03-29
owner: Priority Hub Team
tags: [design, ui, blazor, connectors, emoji]
---

# Introduction

Add a user-selectable emoji to every connector instance so that work items originating from different data sources are instantly distinguishable in the dashboard item list, connector panels, filters, and settings pages. The emoji is persisted per connection instance and rendered at 24 px before each item title, with sensible per-connector-type defaults.

## 1. Purpose & Scope

**Purpose**: Let users assign a Unicode emoji to each configured connector instance and display that emoji prominently next to every work item. This provides a fast, at-a-glance visual cue for the data source of each item.

**Scope**:
- New `Emoji` field on every connection configuration model.
- Static Unicode emoji dataset compiled into the Blazor app (pure C#, no JS interop dependency for the picker).
- Reusable `EmojiPicker` Blazor component with categorised groups and keyword search.
- Emoji display on: work-item cards, connector status cards, settings connector headers, and the upcoming Connectors multi-select filter dropdown.
- Persistence through existing `ProviderConfiguration` JSON serialization.

**Intended audience**: Coding agents and developers implementing this feature.

**Assumptions**:
- The codebase is Blazor Server with interactive server render mode.
- A future change will replace the current single-select "Provider" filter with a multi-select "Connectors" filter; this spec covers emoji integration with both the current and future filter.
- No JS interop is required for the emoji picker; the static dataset and search are handled in C#.

## 2. Definitions

| Term | Definition |
|---|---|
| **Connector type** | A registered `IConnector` implementation (e.g., GitHub, Jira, Azure DevOps). Identified by `ProviderKey`. |
| **Connector instance** (connection) | A single configured connection within a connector type (e.g., "GitHub – Work repo"). Each has a unique `Id`. |
| **Emoji** | A single Unicode emoji character (or grapheme cluster) from the standard Unicode set, e.g., 🐙, 📋. |
| **Emoji dataset** | A static C# class containing the full categorised list of standard Unicode emojis with search keywords. |
| **EmojiPicker** | A reusable Blazor component that renders a searchable, categorised dropdown for emoji selection. |
| **Default emoji** | A sensible emoji pre-assigned to new connections of a given connector type. |
| **BFF** | Backend-for-frontend — the shared ASP.NET Core host running both API and UI. |

## 3. Requirements, Constraints & Guidelines

### Data Model

- **REQ-001**: Every connection model (`AzureDevOpsConnection`, `GitHubConnection`, `JiraConnection`, `TrelloConnection`, `MicrosoftTasksConnection`, `OutlookFlaggedMailConnection`) must include a new `string Emoji` property.
- **REQ-002**: The `Emoji` property must default to the connector type's default emoji (see REQ-010) when a new connection is created.
- **REQ-003**: The `Emoji` property must serialize to and deserialize from the JSON configuration file alongside all other connection properties (camelCase: `"emoji"`).
- **REQ-004**: The `WorkItem` model must include a new `string Emoji` property so the dashboard receives the emoji associated with the source connector instance.
- **REQ-005**: The `BoardConnection` model must include a new `string Emoji` property so the connector status cards receive the emoji.

### Emoji Dataset

- **REQ-006**: Provide a static C# class (`EmojiDataset`) in `backend/PriorityHub.Ui/Services/` containing standard Unicode emojis.
- **REQ-007**: Emojis in the dataset must be grouped into categories matching common mobile keyboard groups: Smileys & People, Animals & Nature, Food & Drink, Travel & Places, Activities, Objects, Symbols, Flags.
- **REQ-008**: Each emoji entry must include: the Unicode character(s), a short display name (English), and a set of lowercase search keywords (at least the display name words).
- **REQ-009**: The dataset must contain at least 200 commonly used emojis across all categories. It does not need to include every Unicode emoji — focus on the most recognisable and commonly used set (equivalent to what appears on a standard mobile emoji keyboard's first pages).

### Default Emojis

- **REQ-010**: Each connector type must have a default emoji:

  | Connector Type | `ProviderKey` | Default Emoji |
  |---|---|---|
  | Azure DevOps | `azure-devops` | 🔷 |
  | GitHub | `github` | 🐙 |
  | Jira | `jira` | 📋 |
  | Trello | `trello` | 📌 |
  | Microsoft Tasks | `microsoft-tasks` | ✅ |
  | Outlook Flagged Mail | `outlook-flagged-mail` | 📧 |

- **REQ-011**: The default emoji lookup must be defined in a single place (e.g., a static dictionary or method on `IConnector` / `ConnectorRegistry`) to avoid duplication.

### EmojiPicker Component

- **REQ-012**: Create a reusable Blazor component `EmojiPicker.razor` in `backend/PriorityHub.Ui/Components/`.
- **REQ-013**: The picker must render as a button showing the currently selected emoji. Clicking the button opens a dropdown panel.
- **REQ-014**: The dropdown must contain a text search input at the top. Typing filters emojis by keyword match (case-insensitive substring on the keywords and display name).
- **REQ-015**: Below the search input, emojis must be displayed in categorised groups with category headings. When a search is active, only matching emojis are shown (still grouped by category, hiding empty categories).
- **REQ-016**: Each emoji in the grid must be rendered at a comfortable tappable size (minimum 32 px for the picker grid items) so they are easy to click/tap.
- **REQ-017**: Clicking an emoji in the grid must select it, close the dropdown, and invoke an `EventCallback<string>` parameter (`OnEmojiSelected`).
- **REQ-018**: The picker must close when clicking outside of it. Reuse the existing `PriorityHub.registerOutsideClick` JS interop pattern already used by `TagFilter`.
- **REQ-019**: The component must accept parameters: `string SelectedEmoji` (current value), `EventCallback<string> OnEmojiSelected` (change callback).
- **REQ-020**: The dropdown must have a max-height with vertical scroll so it does not overflow the viewport.

### Settings Page Integration

- **REQ-021**: In the connector connection editor card on the Settings page, render the `EmojiPicker` component inside each connection card, near the connection name.
- **REQ-022**: When the user selects an emoji in the picker, update the connection's `emoji` field in the in-memory dictionary and save it when "Save connector configuration" is clicked.
- **REQ-023**: The emoji must appear in the connector section header on the Settings page, next to the connector type display name.

### Dashboard Item List Display

- **REQ-024**: In the work-item card on the dashboard, render the item's emoji immediately before the `<h3>` title text inside `div.work-item-title`.
- **REQ-025**: The emoji must render at 24 px (using `font-size: 24px` or equivalent) so it is easily recognisable.
- **REQ-026**: The emoji element must have a `title` attribute showing the connector instance name (e.g., `title="GitHub – Priority Hub Issues"`) for hover identification.
- **REQ-027**: If an item has no emoji (empty or null), do not render a placeholder; simply show the title without an emoji prefix.

### Dashboard Connector Status Cards

- **REQ-028**: In the compact connector card (`article.connector-card`), render the connection's emoji before the board name in `div.connector-topline`.
- **REQ-029**: Use the same 24 px sizing as item cards.

### Dashboard Filters (Current and Future)

- **REQ-030**: In the current Provider filter dropdown, prepend the default connector-type emoji before each provider name in the `<option>` text.
- **REQ-031**: When the Connectors multi-select filter replaces the Provider filter (future change), each selectable connector instance must show its configured emoji before the connection name.

### Connector Pipeline (Emoji Propagation)

- **REQ-032**: When `DashboardAggregator` creates `WorkItem` objects from connector results, it must set `WorkItem.Emoji` to the emoji value from the connection configuration that produced the item.
- **REQ-033**: When `DashboardAggregator` creates `BoardConnection` objects, it must set `BoardConnection.Emoji` to the emoji value from the connection configuration.
- **REQ-034**: Connectors (`IConnector` implementations) do not need to be modified — emoji propagation happens in the aggregator layer where the connection config is available.

### Security

- **SEC-001**: The emoji field must only accept valid Unicode emoji characters. Validate on save that the string is a single Unicode grapheme cluster and is present in the `EmojiDataset`. Reject arbitrary text or HTML.
- **SEC-002**: Emoji rendering in Blazor uses `@` text interpolation (auto-encoded), so there is no XSS risk from the emoji field. Do not use `MarkupString` for emoji rendering.

### Constraints

- **CON-001**: No JavaScript libraries or npm packages for the emoji picker. The implementation must be pure C#/Blazor with a compiled-in static dataset.
- **CON-002**: The existing `PriorityHub.registerOutsideClick` JS interop (already in use by `TagFilter`) may be reused for dropdown dismiss behaviour.
- **CON-003**: The emoji dataset class must be a single static file — do not load emoji data from external URLs or APIs at runtime.
- **CON-004**: The `Emoji` field must not break existing configuration files. Deserialization of JSON that lacks the `"emoji"` property must fall back to the connector type's default emoji.

### Guidelines

- **GUD-001**: Follow existing Blazor component patterns: `@rendermode InteractiveServer`, parameter naming (PascalCase), DI injection.
- **GUD-002**: Follow existing CSS class naming conventions (lowercase-hyphenated). Add emoji-specific styles to the existing stylesheets.
- **GUD-003**: Keep the `EmojiDataset` file generated/maintainable: use a clear structure (static readonly arrays or lists) with comments per category.
- **GUD-004**: Use the `ConnectorFieldSpec` mechanism for the emoji field where possible, or handle it as a special UI-only field in the Settings page if `InputKind` extension is simpler.

### Patterns

- **PAT-001**: Follow the `TagFilter` component pattern for dropdown open/close, outside-click dismiss, and keyboard navigation.
- **PAT-002**: Follow the existing `SetConnectionField` / `GetConnectionField` pattern in `SettingsPage.razor` for reading and writing the emoji value.
- **PAT-003**: Follow the existing `AddConnection` method pattern to set the default emoji when creating a new connection instance.

## 4. Interfaces & Data Contracts

### Updated Connection Models (all in `ConfigModels.cs`)

```csharp
// Added to each connection class:
public string Emoji { get; set; } = ""; // Default set per type in AddConnection
```

Example for `GitHubConnection`:
```csharp
public sealed class GitHubConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;  // NEW
    public string Owner { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string PersonalAccessToken { get; set; } = string.Empty;
    public string Query { get; set; } = "is:open assignee:@me";
    public bool Enabled { get; set; } = true;
}
```

### Updated `WorkItem` Model

```csharp
public sealed class WorkItem
{
    // ... existing properties ...
    public string Emoji { get; set; } = string.Empty;  // NEW — set by aggregator
}
```

### Updated `BoardConnection` Model

```csharp
public sealed class BoardConnection
{
    // ... existing properties ...
    public string Emoji { get; set; } = string.Empty;  // NEW — set by aggregator
}
```

### EmojiDataset Static Class

```csharp
namespace PriorityHub.Ui.Services;

public static class EmojiDataset
{
    public static IReadOnlyList<EmojiCategory> Categories { get; } = [ ... ];

    /// <summary>
    /// Search emojis by keyword substring match. Returns matching entries
    /// grouped by their original category (empty categories excluded).
    /// </summary>
    public static IReadOnlyList<EmojiCategory> Search(string query);
}

public sealed record EmojiCategory(string Name, IReadOnlyList<EmojiEntry> Emojis);

public sealed record EmojiEntry(string Character, string Name, string[] Keywords);
```

### Default Emoji Lookup

```csharp
// In ConnectorRegistry or a new static helper:
public static string GetDefaultEmoji(string providerKey) => providerKey switch
{
    "azure-devops" => "🔷",
    "github"       => "🐙",
    "jira"         => "📋",
    "trello"       => "📌",
    "microsoft-tasks"     => "✅",
    "outlook-flagged-mail" => "📧",
    _ => "📎"
};
```

### EmojiPicker Component Parameters

```csharp
[Parameter, EditorRequired] public string SelectedEmoji { get; set; } = "";
[Parameter, EditorRequired] public EventCallback<string> OnEmojiSelected { get; set; }
```

### JSON Configuration (extended)

```json
{
  "github": [
    {
      "id": "github-priority-hub",
      "name": "Priority Hub Issues",
      "emoji": "🐙",
      "owner": "your-org-or-user",
      "repository": "your-repository",
      "personalAccessToken": "replace-with-pat",
      "query": "is:open assignee:@me",
      "enabled": true
    }
  ]
}
```

## 5. Acceptance Criteria

- **AC-001**: Given a new connection is added on the Settings page, When the user clicks "Add connection" for a connector type, Then the new connection's emoji field is pre-populated with the connector type's default emoji (e.g., 🐙 for GitHub).
- **AC-002**: Given a connection card on the Settings page, When the user clicks the emoji button, Then a dropdown appears showing categorised emojis with a search input at the top.
- **AC-003**: Given the emoji picker is open, When the user types "rocket" in the search input, Then only emojis matching "rocket" keyword are displayed.
- **AC-004**: Given the emoji picker is open with search results, When the user clicks an emoji, Then the picker closes and the connection's emoji updates to the selected character.
- **AC-005**: Given a connection with a custom emoji is saved, When the configuration JSON is inspected, Then the `"emoji"` field contains the selected Unicode character.
- **AC-006**: Given a configuration JSON that does not contain an `"emoji"` field for a connection, When it is loaded, Then the connection uses the connector type's default emoji.
- **AC-007**: Given items are displayed in the dashboard work queue, When a work item card renders, Then the item's emoji appears at 24 px immediately before the title text.
- **AC-008**: Given the emoji is rendered on a work item card, When the user hovers over the emoji, Then a tooltip shows the connector instance name.
- **AC-009**: Given the compact connector cards section is expanded, When a connector card renders, Then the connection's emoji appears before the board name.
- **AC-010**: Given the Provider filter dropdown (current), When the dropdown options render, Then each provider option is prefixed with the connector type's default emoji.
- **AC-011**: Given the future Connectors multi-select filter, When each connector instance option renders, Then it shows the instance's configured emoji before the connection name.
- **AC-012**: Given the emoji picker dropdown is open, When the user clicks outside the dropdown, Then the dropdown closes.
- **AC-013**: Given a user attempts to save a connection with an invalid emoji value (not in the dataset), When save is triggered, Then validation rejects the value and shows an error.

## 6. Test Automation Strategy

- **Test Levels**: Unit tests, component rendering tests (bUnit).
- **Frameworks**: MSTest, FluentAssertions, bUnit (for Blazor component tests).

### Unit Tests (`PriorityHub.Api.Tests/`)

| Test | Coverage |
|---|---|
| `EmojiDataset` returns non-empty categories | REQ-007 |
| `EmojiDataset.Search("rocket")` returns matching entries | REQ-008 |
| `EmojiDataset.Search("")` returns all entries | REQ-014 |
| `GetDefaultEmoji` returns correct emoji per provider key | REQ-010 |
| `GetDefaultEmoji` returns fallback for unknown key | REQ-010 |
| Connection model serialization round-trips `Emoji` | REQ-003 |
| Missing `emoji` JSON property deserializes to empty string | CON-004 |

### Component Tests (`PriorityHub.Ui.Tests/Components/`)

| Test | Coverage |
|---|---|
| `EmojiPicker` renders selected emoji on button | REQ-013 |
| `EmojiPicker` opens dropdown on click | REQ-013 |
| `EmojiPicker` filters emojis when search text entered | REQ-014 |
| `EmojiPicker` invokes callback with selected emoji | REQ-017 |
| `EmojiPicker` groups emojis by category | REQ-015 |
| Dashboard item card renders emoji before title | REQ-024 |
| Dashboard item card renders emoji at correct size class | REQ-025 |
| Dashboard connector card renders emoji | REQ-028 |

### CI/CD Integration

- All tests run via `dotnet test PriorityHub.sln` in existing GitHub Actions pipeline.
- No additional test infrastructure required.

## 7. Rationale & Context

Users managing work items from 4–6 connector instances struggle to visually distinguish item origins at a glance. The current UI shows the provider name in text form only in the filter dropdown and connector cards — but not on individual work item cards.

Emoji provides a universally understood, compact, cross-platform visual marker. A per-instance (rather than per-type) scope allows users with multiple connections of the same type (e.g., two Azure DevOps orgs) to differentiate them visually.

A pure C# static dataset was chosen over a JS interop library to:
1. Avoid adding an npm/JS dependency to a Blazor Server app that otherwise has no JS build pipeline.
2. Reduce SignalR traffic (the entire picker state is server-side, only click events travel the wire).
3. Keep bundle size predictable — the dataset compiles into the assembly.

The 24 px size was chosen to be large enough for instant recognition in the item list without dominating the card layout (the `<h3>` title is the primary visual element).

## 8. Dependencies & External Integrations

### Technology Platform Dependencies

- **PLT-001**: .NET 10 / Blazor Server — no additional runtime requirements.
- **PLT-002**: Unicode emoji support in the user's browser and OS. All modern browsers and operating systems (Windows 10+, macOS, Linux with emoji fonts, iOS, Android) render standard Unicode emojis natively. No web font or image sprite required.

### Internal Dependencies

- **INT-001**: Existing `PriorityHub.registerOutsideClick` JS interop function — reused by `EmojiPicker` for dropdown dismiss (same pattern as `TagFilter`).
- **INT-002**: `ConnectorRegistry` — extended with a static `GetDefaultEmoji` method (or equivalent).
- **INT-003**: `DashboardAggregator` — must propagate emoji from connection config to `WorkItem` and `BoardConnection` models.
- **INT-004**: Upcoming Connectors multi-select filter (separate spec/issue) — emoji integration is defined here but implementation depends on that filter existing.

### Data Dependencies

- **DAT-001**: Static Unicode emoji dataset — sourced from the Unicode CLDR common emoji list (public domain). Curated to ~200–400 commonly used emojis with English keywords. Compiled into the assembly as a static C# class.

## 9. Examples & Edge Cases

### Example: Default emoji applied on new connection

```csharp
// In SettingsPage.razor AddConnection method:
var dict = new Dictionary<string, object?>
{
    ["id"] = Guid.NewGuid().ToString("N"),
    ["enabled"] = true,
    ["emoji"] = ConnectorRegistry.GetDefaultEmoji(connector.ProviderKey)
};
```

### Example: Emoji in work item card

```html
<!-- Rendered output -->
<div class="work-item-title">
    <span class="item-emoji" title="GitHub – Priority Hub Issues"
          style="font-size: 24px;">🐙</span>
    <h3>Fix login redirect loop</h3>
</div>
```

### Example: Emoji in connector status card

```html
<div class="connector-topline">
    <span class="connector-emoji" style="font-size: 24px;">📋</span>
    <strong>Growth Board</strong>
    <span class="sync-pill sync-connected">connected</span>
</div>
```

### Example: Emoji in Provider filter option

```html
<option value="github">🐙 GitHub</option>
<option value="jira">📋 Jira</option>
```

### Edge Cases

| Scenario | Expected Behaviour |
|---|---|
| Legacy config JSON without `"emoji"` field | Deserialization produces `""`. UI/aggregator falls back to `GetDefaultEmoji(providerKey)`. |
| User clears the emoji field | Not allowed — the picker always requires a selection. If somehow empty, fall back to default. |
| Emoji picker search with no results | Show "No emojis match your search" message. |
| Emoji with multi-codepoint grapheme cluster (e.g., flag 🇷🇸, skin tone 👍🏽) | Stored and rendered as-is. The dataset includes these as single entries. Validation checks presence in dataset. |
| Very long connection name in tooltip | Browser truncates tooltip naturally. No special handling needed. |
| Two connections with identical emoji | Allowed — emoji is a visual aid, not a unique identifier. |

## 10. Validation Criteria

1. **Build**: `dotnet build PriorityHub.sln` succeeds with zero errors and zero new warnings.
2. **Tests**: `dotnet test PriorityHub.sln` passes all existing and new tests.
3. **Settings page**: New connections show default emoji; emoji picker opens, searches, selects, and persists.
4. **Dashboard**: Every work item card shows the source connector's emoji at 24 px before the title.
5. **Connector cards**: Emoji appears before the board name in compact connector cards.
6. **Filter dropdown**: Provider filter options are prefixed with connector type emojis.
7. **Config round-trip**: Saving and reloading config preserves custom emoji selections.
8. **Legacy compatibility**: Loading a config file without `"emoji"` fields does not error; defaults are applied.
9. **Security**: Pasting arbitrary text into the emoji field (via devtools or JSON edit) is rejected on save validation.

## 11. Related Specifications / Further Reading

- [Unicode Emoji Charts](https://unicode.org/emoji/charts/full-emoji-list.html) — authoritative list of standard emojis.
- [Unicode CLDR Emoji Annotations](https://cldr.unicode.org/translation/characters-emoji-symbols/emoji-annotations) — source for English emoji keywords.
- [spec-17: Dashboard UX Improvements](plans/specifications/spec-17-feat-dashboard-ux-improvements-icons-gravatar-move-to-top-drag-i.md) — related dashboard display changes.
- Upcoming: Connectors multi-select filter specification (to replace Provider filter).