# Configuration

Priority Hub connectors are configured through the **Settings** page in the UI.
Provider credentials are stored in PostgreSQL (default) or in local JSON files, and are never committed to version control.

## Storage Back-end

The active storage back-end is selected by the `ConfigStore:Provider` key in `appsettings.json`
(or any higher-priority configuration source):

| Value      | Description                                                           | Default environment |
|------------|-----------------------------------------------------------------------|---------------------|
| `Postgres` | Stores per-user config as JSONB in a PostgreSQL database.             | Development         |
| `File`     | Stores per-user config as JSON files in `config/users/` on disk.     | All others          |

### PostgreSQL connection string

When `ConfigStore:Provider` is `Postgres`, you must also supply `ConfigStore:ConnectionString`.

The recommended approach is **user secrets** (never commit connection strings):

```bash
dotnet user-secrets set "ConfigStore:ConnectionString" "Host=localhost;Database=priorityhub;Username=priorityhub;Password=dev_password" \
  --project backend/PriorityHub.Ui
```

For convenience, `appsettings.Development.json` ships with the default docker-compose credentials.
Override this file or use user secrets to connect to a different instance.

### Schema migrations

Migrations live in `backend/PriorityHub.Api/Data/Migrations/` and are embedded in the assembly.

- **Development**: migrations are applied automatically on startup.
- **Other environments**: the application fails fast with a clear error if unapplied migrations are detected.
  Run migrations manually (e.g., using `psql` or a migration tool) before deploying.

## Adding a Connector

1. Open the application and navigate to **Settings**.
2. Click **Add connector** and select the provider type.
3. Fill in the required fields for that provider (see below).
4. Click **Save**.

Expected result: the connector appears in the dashboard on the next poll cycle.

---

## Provider Reference

### Azure DevOps

Fetches work items from an Azure DevOps project using a [WIQL](https://learn.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax) query.

| Field | Required | Description |
|-------|----------|-------------|
| Connection name | ✔ | Display name shown on the dashboard (e.g., `Commerce Platform`) |
| Organization | ✔ | Azure DevOps organization name or URL slug (e.g., `contoso`) |
| Project | ✔ | Team project name (e.g., `Commerce Platform`) |
| Personal Access Token | optional | PAT with **Work Items (Read)** scope. Not required when signed in with Microsoft. See [Create a PAT](https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate) |
| WIQL query | ✔ | Work Item Query Language query. Default returns all open items sorted by last change. |

**WIQL examples**

All open items assigned to you, sorted by changed date:
```wiql
Select [System.Id] From WorkItems
Where [System.TeamProject] = @project
  And [System.State] <> 'Closed'
  And [System.AssignedTo] = @me
Order By [System.ChangedDate] Desc
```

High-priority bugs and tasks in active sprint:
```wiql
Select [System.Id] From WorkItems
Where [System.TeamProject] = @project
  And [System.WorkItemType] In ('Bug', 'Task')
  And [System.State] Not In ('Closed', 'Resolved')
  And [Microsoft.VSTS.Common.Priority] <= 2
  And [System.IterationPath] = @currentIteration('[Commerce Platform]\My Team')
Order By [Microsoft.VSTS.Common.Priority] Asc
```

**External docs:** [WIQL syntax reference](https://learn.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax) · [Create a PAT](https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate)

---

### Jira

Fetches issues from a Jira Cloud project using a [JQL](https://support.atlassian.com/jira-software-cloud/docs/what-is-advanced-searching-in-jira-software/) query.

| Field | Required | Description |
|-------|----------|-------------|
| Connection name | ✔ | Display name shown on the dashboard (e.g., `Growth Board`) |
| Base URL | ✔ | Jira instance URL (e.g., `https://yourorg.atlassian.net`) |
| Email | ✔ | Atlassian account email used for API authentication |
| API token | ✔ | Atlassian API token. See [Manage API tokens](https://support.atlassian.com/atlassian-account/docs/manage-api-tokens-for-your-atlassian-account/) |
| JQL query | ✔ | Jira Query Language query. Default returns open items assigned to you. |

**JQL examples**

Open issues assigned to you across all projects:
```jql
assignee = currentUser() AND statusCategory != Done ORDER BY updated DESC
```

Open issues in a specific project with high or critical priority:
```jql
project = GROWTH
  AND assignee = currentUser()
  AND statusCategory != Done
  AND priority in (High, Critical)
ORDER BY priority DESC, updated DESC
```

Issues with a due date in the next 7 days:
```jql
assignee = currentUser()
  AND statusCategory != Done
  AND due <= 7d
ORDER BY due ASC
```

**External docs:** [JQL reference](https://support.atlassian.com/jira-software-cloud/docs/what-is-advanced-searching-in-jira-software/) · [Manage API tokens](https://support.atlassian.com/atlassian-account/docs/manage-api-tokens-for-your-atlassian-account/)

---

### Trello

Fetches all open cards from a Trello board. No query language is needed; all visible cards on the board are included.

| Field | Required | Description |
|-------|----------|-------------|
| Connection name | ✔ | Display name shown on the dashboard (e.g., `Advisory Pipeline`) |
| Board ID | ✔ | 8-character board identifier from the board URL: `https://trello.com/b/<board-id>/board-name` |
| API key | ✔ | Trello developer API key. See [Trello API key generation](https://trello.com/app-key) |
| Token | ✔ | Trello OAuth token authorizing access to your boards. Generated on the same page as the API key. |

**How to find your Board ID**

Open your Trello board in a browser. The URL is:
```
https://trello.com/b/AbCd1234/my-board-name
                    ^^^^^^^^
                    This is your Board ID
```

**Label-based scoring**

Trello has no built-in priority field. Priority Hub uses card labels to adjust scores:
- A label named `urgent` raises the item's impact and urgency scores.
- A label named `blocked` adds a blocker count of 1.

**External docs:** [Trello API key and token](https://trello.com/app-key) · [Trello REST API reference](https://developer.atlassian.com/cloud/trello/rest/)

---

### GitHub Issues

Fetches issues from a GitHub repository using the [GitHub issue search syntax](https://docs.github.com/en/search-github/searching-on-github/searching-issues-and-pull-requests).

| Field | Required | Description |
|-------|----------|-------------|
| Connection name | ✔ | Display name shown on the dashboard (e.g., `Priority Hub Issues`) |
| Owner or organization | ✔ | GitHub username or organization (e.g., `my-org`) |
| Repository | ✔ | Repository name (e.g., `my-repo`) |
| Personal access token | optional | Fine-grained or classic PAT with **Issues (Read)** permission. Not required when signed in with GitHub. See [Create a PAT](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens) |
| Issue query | optional | GitHub issue search qualifiers. Pull requests are automatically excluded. Default: `is:open assignee:@me` |

**Issue query examples**

Open issues assigned to you (default):
```
is:open assignee:@me
```

Open issues with a specific label:
```
is:open assignee:@me label:bug
```

High-priority issues with no milestone:
```
is:open assignee:@me label:"high priority" no:milestone
```

**Label-based scoring**

Priority Hub uses issue labels to derive impact and urgency scores:
- `critical` or `high` labels → impact score 9
- `low` label → impact score 4
- `urgent`, `p0`, or `p1` labels → urgency score 9
- `blocked` label → blocker count 1

**External docs:** [GitHub issue search syntax](https://docs.github.com/en/search-github/searching-on-github/searching-issues-and-pull-requests) · [Create a PAT](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens)

---

### Microsoft Tasks

Fetches Microsoft To Do tasks via the [Microsoft Graph API](https://learn.microsoft.com/en-us/graph/api/resources/todotask). Requires Microsoft sign-in.

| Field | Required | Description |
|-------|----------|-------------|
| Connection name | ✔ | Display name shown on the dashboard (e.g., `My Tasks`) |
| Task list name | optional | Filter to a specific task list by name. Leave blank to include all task lists. |

**Authentication:** This connector requires signing in with Microsoft. No PAT or API token is needed.

**External docs:** [Microsoft To Do overview](https://support.microsoft.com/en-us/office/getting-started-with-microsoft-to-do-2c570b77-f73e-4701-9b76-6b05a4ee8f59) · [Microsoft Graph Tasks API](https://learn.microsoft.com/en-us/graph/api/resources/todotask)

---

### Outlook Flagged Mail

Fetches flagged (follow-up) email from your Outlook mailbox via the [Microsoft Graph API](https://learn.microsoft.com/en-us/graph/api/resources/message). Requires Microsoft sign-in.

| Field | Required | Description |
|-------|----------|-------------|
| Connection name | ✔ | Display name shown on the dashboard (e.g., `Flagged Mail`) |
| Folder ID | optional | Limit the scan to a specific mail folder. Leave blank to scan all folders. |
| Max results | optional | Maximum number of flagged messages to retrieve (default: `100`). |

**Authentication:** This connector requires signing in with Microsoft. No PAT or API token is needed.

**External docs:** [Flag email for follow-up](https://support.microsoft.com/en-us/office/flag-email-messages-for-follow-up-9d0f175f-f3e9-406d-bbf7-9c57e1f781cc) · [Microsoft Graph Messages API](https://learn.microsoft.com/en-us/graph/api/resources/message)

---

## Local Config File

`config/providers.local.json` holds all connector credentials. See `config/providers.example.json` for a full annotated example.

- This file is gitignored and must never be committed.
- If the file does not exist the app starts with an empty dashboard so you can add connectors via Settings.
- Back up the file manually if you need to preserve your configuration.

**Example structure:**

```json
{
  "preferences": { "orderedItemIds": [] },
  "azureDevOps": [
    {
      "id": "ado-platform",
      "name": "Commerce Platform",
      "organization": "contoso",
      "project": "Commerce Platform",
      "personalAccessToken": "<your-pat>",
      "wiql": "Select [System.Id] From WorkItems Where [System.TeamProject] = @project And [System.State] <> 'Closed' Order By [System.ChangedDate] Desc",
      "enabled": true
    }
  ],
  "jira": [
    {
      "id": "jira-growth",
      "name": "Growth Board",
      "baseUrl": "https://yourorg.atlassian.net",
      "email": "you@example.com",
      "apiToken": "<your-api-token>",
      "jql": "project = GROWTH AND assignee = currentUser() AND statusCategory != Done ORDER BY priority DESC, updated DESC",
      "enabled": true
    }
  ],
  "trello": [
    {
      "id": "trello-pipeline",
      "name": "Advisory Pipeline",
      "boardId": "AbCd1234",
      "apiKey": "<your-api-key>",
      "token": "<your-token>",
      "enabled": true
    }
  ]
}
```

Replace placeholder values (`<your-pat>`, etc.) with real credentials. Never commit this file.

## Related

- [Features](../features/README.md) – how connector data is scored and ranked.
- [Troubleshooting](../troubleshooting/README.md) – resolve missing or invalid connector configuration.

