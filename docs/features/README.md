# Features

Priority Hub provides a unified personal priority view across multiple work-item providers.

## Dashboard

The dashboard aggregates work items from all configured connectors into a single ranked list.

- **Normalized model** – items from Azure DevOps, Jira, and Trello share a common schema.
- **Ranked queue** – items are ordered by impact, urgency, confidence, blockers, age, and effort.
- **Connector health** – each provider's connection status is visible on the dashboard.
- **Filtering** – filter items by provider, label, or status.

## Priority Ordering

- Items are ranked automatically using the built-in scoring formula.
- Manual drag-and-drop lets you override the automatic order.
- Manual positions persist across sessions and all sources.
- New items are highlighted until you place them in your preferred order.

## Multi-Connector Model

Multiple instances of the same provider can run in parallel.

- Add several Azure DevOps projects, Jira boards, or Trello boards.
- Each connector is polled independently.
- All connectors feed into the single priority view.

## Related

- [Configuration](../configuration/README.md) – how to add and manage connectors.
- [Troubleshooting](../troubleshooting/README.md) – resolve empty dashboard or connection issues.
