# Configuration

Priority Hub connectors are configured through the **Settings** page in the UI.
Credentials are stored in `config/providers.local.json` on the server and are never committed to version control.

## Adding a Connector

1. Open the application and navigate to **Settings**.
2. Select the provider type (Azure DevOps, Jira, or Trello).
3. Fill in the required fields for that provider (see below).
4. Click **Save**.

Expected result: the connector appears in the dashboard aggregation on the next poll cycle.

## Provider Fields

### Azure DevOps

| Field | Description |
|-------|-------------|
| Connection name | Display name for this connector instance |
| Organization | Azure DevOps organization URL or name |
| Project | Team project name |
| Personal Access Token (PAT) | PAT with work item read permissions |
| WIQL query | Work Item Query Language query to fetch items |

### Jira

| Field | Description |
|-------|-------------|
| Connection name | Display name for this connector instance |
| Base URL | Jira instance URL (e.g., `https://yourorg.atlassian.net`) |
| Email | Account email used for API authentication |
| API token | Atlassian API token |
| JQL query | Jira Query Language query to fetch issues |

### Trello

| Field | Description |
|-------|-------------|
| Connection name | Display name for this connector instance |
| Board ID | Trello board identifier |
| API key | Trello developer API key |
| Token | Trello OAuth token |

## Local Config File

`config/providers.local.json` holds all connector credentials.

- This file is gitignored and must never be committed.
- If the file does not exist the app starts with an empty dashboard so you can add connectors via Settings.
- Back up the file manually if you need to preserve your configuration.

## Related

- [Troubleshooting](../troubleshooting/README.md) – resolve missing or invalid connector configuration.
