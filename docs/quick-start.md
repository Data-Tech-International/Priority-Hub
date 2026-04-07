# Quick Start

Sign in, connect your work-item sources, and start triaging — all from the browser.

> **For developers and administrators:** see the [project README](../README.md) for installation, build, and deployment instructions.

## Step 1: Sign in

Open Priority Hub in your browser and click a sign-in button (e.g., Microsoft or GitHub).
You will be redirected to your provider's login screen and back to Priority Hub once authenticated.

## Step 2: Add your first connector

1. Click the **Settings** gear icon in the top navigation bar.
2. Expand a provider section (e.g., Azure DevOps, Jira, GitHub Issues, Trello, Microsoft Tasks, or Outlook Flagged Mail).
3. Click **Add connection**.
4. Fill in the required fields. Each provider section includes a help panel — click it for field descriptions and tips.

Below are the fields for each provider with example values.

### Azure DevOps

| Field | Required | Example value |
|-------|----------|---------------|
| Connection name | ✔ | `My Project` |
| Organization | ✔ | `contoso` |
| Project | ✔ | `Commerce Platform` |
| Personal Access Token | | *(not needed when signed in with Microsoft)* |
| WIQL query | ✔ | `Select [System.Id] From WorkItems Where [System.TeamProject] = @project And [System.State] <> 'Closed' And [System.AssignedTo] = @me Order By [System.ChangedDate] Desc` |

If you use a PAT, it needs **Work Items (Read)** scope. [Create a PAT](https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate) · [WIQL syntax](https://learn.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax)

### GitHub Issues

| Field | Required | Example value |
|-------|----------|---------------|
| Connection name | ✔ | `My Repo Issues` |
| Owner / organization | ✔ | `my-org` |
| Repository | ✔ | `my-repo` |
| Personal access token | | *(not needed when signed in with GitHub)* |
| Issue query | | `is:open assignee:@me` |

If you use a PAT, it needs **Issues (Read)** permission. [Create a PAT](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens) · [Search syntax](https://docs.github.com/en/search-github/searching-on-github/searching-issues-and-pull-requests)

### Jira

| Field | Required | Example value |
|-------|----------|---------------|
| Connection name | ✔ | `Growth Board` |
| Base URL | ✔ | `https://yourorg.atlassian.net` |
| Email | ✔ | `you@example.com` |
| API token | ✔ | *(from your Atlassian account)* |
| JQL query | ✔ | `assignee = currentUser() AND statusCategory != Done ORDER BY updated DESC` |

[Manage API tokens](https://support.atlassian.com/atlassian-account/docs/manage-api-tokens-for-your-atlassian-account/) · [JQL reference](https://support.atlassian.com/jira-software-cloud/docs/what-is-advanced-searching-in-jira-software/)

### Trello

| Field | Required | Example value |
|-------|----------|---------------|
| Connection name | ✔ | `Advisory Pipeline` |
| Board ID | ✔ | `AbCd1234` *(the 8-character code in your board URL)* |
| API key | ✔ | *(from Trello developer portal)* |
| Token | ✔ | *(generated on the same page as the API key)* |

[Get your API key and token](https://trello.com/app-key)

### Microsoft Tasks

| Field | Required | Example value |
|-------|----------|---------------|
| Connection name | ✔ | `My Tasks` |
| Task list name | | *(leave blank for all lists)* |

Requires Microsoft sign-in. No API token needed.

### Outlook Flagged Mail

| Field | Required | Example value |
|-------|----------|---------------|
| Connection name | ✔ | `Flagged Mail` |
| Folder ID | | *(leave blank for all folders)* |
| Max results | | `100` |

Requires Microsoft sign-in. No API token needed.

5. Click **Save**. The connector appears on the dashboard on the next refresh.

> **Tip:** You can add multiple connections for the same provider (e.g., two Azure DevOps projects). All of them feed into the same priority view.

## Step 3: Use the dashboard

Navigate to **Dashboard**. Your work items from all connected sources appear in a single ranked list, grouped into three priority bands:

| Band | Score | Meaning |
|------|-------|---------|
| **Critical** | 82–100 | Needs immediate attention |
| **Focus** | 60–81 | Tackle in the current cycle |
| **Maintain** | 0–59 | Keep moving or defer |

### Reorder items

Drag and drop any item to override the automatic score-based order. Your manual position is saved and persists across sessions.

Items added since your last reorder are highlighted with a **New** badge so you can place them where they belong.

### Filter the view

Use the toolbar filters to narrow the list:

- **Connectors** — show items from specific providers.
- **Tags** — filter by label or tag.
- **Blocked** — show all, blocked only, or not blocked.
- **Target date** — all, has date, no date, overdue, or due within 7 days.

### Understand the cards

Each work item card shows:

- **Title** and link to the original item in its source system.
- **Provider icon** and connection name.
- **Priority score** with the color-coded band.
- **Status** normalized across providers (Planned, In Progress, Review, Blocked, Done).
- **Blocked pill** (red) when the item is flagged as blocked in its source.
- **Target date** with a countdown badge: red if overdue, amber if due within 7 days.

## Step 4: Manage your configuration

### Edit or remove a connector

Go to **Settings**, expand the provider section, and edit the fields or click **Remove** (a confirmation dialog will appear before deletion).

### Export and import

Use the **Import / Export** tab in Settings to:

- **Download** your full configuration as a JSON file for backup.
- **Import** a previously exported file to restore or migrate connections.

## What's next?

- See [Features](features/README.md) for the full scoring formula, status mapping, and multi-connector details.
- See [Configuration](configuration/README.md) for advanced query examples and the complete field reference.
- If you are deploying in Azure, enable optional Application Insights telemetry by configuring `APPLICATIONINSIGHTS_CONNECTION_STRING` (see [Configuration](configuration/README.md#azure-monitor-telemetry-optional)).
- Check [Troubleshooting](troubleshooting/README.md) if the dashboard is empty or a connector shows an error.
