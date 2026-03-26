# Features

Priority Hub provides a unified personal priority view across multiple work-item providers.

## Dashboard

The dashboard aggregates work items from all configured connectors into a single ranked list.

- **Normalized model** – items from all providers share a common schema (title, status, assignee, tags, scores).
- **Ranked queue** – items are ordered by a composite priority score (see [Scoring formula](#scoring-formula) below).
- **Priority bands** – items are grouped into Critical, Focus, and Maintain bands for at-a-glance triage.
- **Connector health** – each provider's connection status is visible at the top of the dashboard.
- **Filtering** – filter items by provider, label, or status using the toolbar.

## Supported Connectors

| Connector | Auth method | Query language |
|-----------|-------------|----------------|
| Azure DevOps | PAT or Microsoft sign-in | WIQL |
| Jira | API token + email | JQL |
| Trello | API key + token | None (all open cards) |
| GitHub Issues | PAT or GitHub sign-in | Issue search qualifiers |
| Microsoft Tasks | Microsoft sign-in | None (task list filter only) |
| Outlook Flagged Mail | Microsoft sign-in | None (folder filter only) |

For setup details see [Configuration](../configuration/README.md).

## Priority Ordering

### Scoring Formula

Every work item receives a numeric priority score (0–100):

```
Score = Impact × 4
      + Urgency × 3
      + Confidence × 1.5
      + min(AgeDays, 10)          ← age contribution capped at 10
      + BlockerCount × 4
      + DueDateWeight             ← bonus for items due within 14 days
      − Effort × 2
```

Clamped to the range **0–100**.

**Field descriptions:**

| Field | Range | Source |
|-------|-------|--------|
| Impact | 1–10 | Derived from provider priority (ADO/Jira priority field, GitHub/Trello labels) |
| Urgency | 1–10 | Derived from provider severity or labels |
| Confidence | 1–10 | Fixed at 7 for most items (reflects how well the data maps) |
| AgeDays | ≥0 | Days since the item was last updated |
| BlockerCount | ≥0 | Number of blocker/dependency relationships |
| DueDateWeight | 0–10 | Bonus applied when `DueInDays` is ≤ 14 |
| Effort | 1–10 | Derived from story points (ADO) or fixed default per provider |

### Priority Bands

| Band | Score range | Meaning |
|------|-------------|---------|
| **Critical** | ≥ 82 | High-impact, urgent items that need immediate attention |
| **Focus** | 60–81 | Important items to tackle in the current cycle |
| **Maintain** | < 60 | Low-risk items to keep moving or defer |

### Manual Ordering

- Drag and drop any item to override the automatic score-based order.
- Manual positions persist across sessions and all sources.
- Items added since your last manual reorder are highlighted as **New** until you place them.

## Multi-Connector Model

Multiple instances of the same provider can run in parallel.

- Add several Azure DevOps projects, Jira boards, or Trello boards.
- Each connector is polled independently on a background schedule.
- All connectors feed into the single priority view.

**Example:** You can add one Azure DevOps connector for your main project and a second for a shared-services project. Both appear together in the ranked queue.

## Status Mapping

Work item statuses from each provider are normalized to a shared set:

| Normalized status | Meaning | Example raw values |
|------------------|---------|-------------------|
| `planned` | Not yet started | New, To Do, Backlog, Open |
| `in-progress` | Actively being worked | Active, In Progress, Doing |
| `review` | Waiting for review or QA | Review, QA, Validate |
| `blocked` | Blocked by a dependency | Blocked |
| `done` | Completed | Done, Closed, Resolved, Complete |

## Related

- [Configuration](../configuration/README.md) – how to add and manage connectors.
- [Troubleshooting](../troubleshooting/README.md) – resolve empty dashboard or connection issues.

